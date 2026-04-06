using ImageRecognition.Application.Interfaces;
using ImageRecognition.Application.Services;
using ImageRecognition.Infrastructure.Database;
using Microsoft.Win32;
using Npgsql;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Windows;

namespace ImageRecognition.UI;

public partial class MainWindow : Window
{
    private IRecognitionService? _recognitionService;
    private bool _isDatabaseConnected;

    public MainWindow()
    {
        InitializeComponent();
        SetWorkPanelEnabled(false);
    }

    private void AppendLog(string message)
    {
        LogTextBox.AppendText($"{DateTime.Now:HH:mm:ss} | {message}{Environment.NewLine}");
        LogTextBox.ScrollToEnd();
    }

    private void SetWorkPanelEnabled(bool enabled)
    {
        WorkPanel.IsEnabled = enabled;
        ClassNameTextBox.IsEnabled = enabled;
        TrainingImagePathTextBox.IsEnabled = enabled;
        InferenceImagePathTextBox.IsEnabled = enabled;
        InitializeSchemaButton.IsEnabled = enabled;
        GenerateDemoButton.IsEnabled = enabled;
    }

    private bool EnsureConnected()
    {
        if (!_isDatabaseConnected || _recognitionService is null)
        {
            AppendLog("ERROR: Сначала подключитесь к существующей БД.");
            return false;
        }

        return true;
    }

    private async void ConnectDbButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (!int.TryParse(DbPortTextBox.Text, out int port) || port <= 0)
            {
                AppendLog("ERROR: Некорректный порт БД.");
                return;
            }

            var options = new PostgresOptions
            {
                Host = DbHostTextBox.Text.Trim(),
                Port = port,
                Database = DbNameTextBox.Text.Trim(),
                Username = DbUserTextBox.Text.Trim(),
                Password = DbPasswordTextBox.Text
            };

            _recognitionService = ApplicationFactory.CreateDefaultRecognitionService(options);
            using var connection = new NpgsqlConnection(options.ToConnectionString());
            await connection.OpenAsync();

            await _recognitionService.InitializeDatabaseAsync();

            _isDatabaseConnected = true;
            SetWorkPanelEnabled(true);
            ConnectionStatusTextBlock.Text = $"БД: подключено ({options.Host}:{options.Port}/{options.Database})";
            ConnectionStatusTextBlock.Foreground = System.Windows.Media.Brushes.DarkGreen;
            AppendLog("Подключение к существующей БД успешно. Таблицы проверены.");
            await RefreshDatabaseStatsAsync();
        }
        catch (Exception ex)
        {
            _isDatabaseConnected = false;
            SetWorkPanelEnabled(false);
            ConnectionStatusTextBlock.Text = "БД: ошибка подключения";
            ConnectionStatusTextBlock.Foreground = System.Windows.Media.Brushes.DarkRed;
            AppendLog($"ERROR: {ex.Message}");
        }
    }

    private async void InitializeDatabaseButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (!EnsureConnected()) return;
            await _recognitionService!.InitializeDatabaseAsync();
            AppendLog("Database initialized (tables created if not exists).");
            await RefreshDatabaseStatsAsync();
        }
        catch (Exception ex)
        {
            AppendLog($"ERROR: {ex.Message}");
        }
    }

    private void BrowseTrainingImageButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Filter = "Image files|*.png;*.jpg;*.jpeg;*.bmp;*.gif|All files|*.*"
        };

        if (dialog.ShowDialog() == true)
        {
            TrainingImagePathTextBox.Text = dialog.FileName;
        }
    }

    private void BrowseInferenceImageButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Filter = "Image files|*.png;*.jpg;*.jpeg;*.bmp;*.gif|All files|*.*"
        };

        if (dialog.ShowDialog() == true)
        {
            InferenceImagePathTextBox.Text = dialog.FileName;
        }
    }

    private async void AddTrainingImageButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (!EnsureConnected()) return;
            if (!TryValidateImagePath(TrainingImagePathTextBox.Text, out string filePath)) return;
            if (string.IsNullOrWhiteSpace(ClassNameTextBox.Text))
            {
                AppendLog("ERROR: Class name is required.");
                return;
            }

            int imageId = await _recognitionService!.AddTrainingImageAsync(filePath, ClassNameTextBox.Text);
            AppendLog($"Training image saved to DB (BYTEA). ImageId={imageId}, class={ClassNameTextBox.Text}");
            await RefreshDatabaseStatsAsync();
        }
        catch (Exception ex)
        {
            AppendLog($"ERROR: {ex.Message}");
        }
    }

    private async void TrainModelButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (!EnsureConnected()) return;
            if (!TryReadK(out int k)) return;
            var result = await _recognitionService!.TrainAsync(ModelNameTextBox.Text, k);
            AppendLog($"Model trained. ModelId={result.ModelId}, samples={result.SampleCount}, build={result.BuildTime.TotalMilliseconds:F2} ms");
        }
        catch (Exception ex)
        {
            AppendLog($"ERROR: {ex.Message}");
        }
    }

    private async void ClassifyKdTreeButton_Click(object sender, RoutedEventArgs e)
    {
        await ClassifyAsync(useKdTree: true);
    }

    private async void ClassifyLinearButton_Click(object sender, RoutedEventArgs e)
    {
        await ClassifyAsync(useKdTree: false);
    }

    private async Task ClassifyAsync(bool useKdTree)
    {
        try
        {
            if (!EnsureConnected()) return;
            if (!TryReadK(out int k)) return;
            if (!TryValidateImagePath(InferenceImagePathTextBox.Text, out string filePath)) return;

            var result = await _recognitionService!.ClassifyImageAsync(filePath, k, useKdTree);
            LastResultTextBlock.Text = $"Класс: {result.PredictedClassName} (id={result.PredictedClassId}), метод={(useKdTree ? "KDTree" : "Linear")}";
            AppendLog($"Classified ({(useKdTree ? "KDTree" : "Linear")}): class={result.PredictedClassName}, id={result.PredictedClassId}, search={result.SearchTime.TotalMilliseconds:F2} ms");
        }
        catch (Exception ex)
        {
            AppendLog($"ERROR: {ex.Message}");
        }
    }

    private async void BenchmarkButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (!EnsureConnected()) return;
            if (!TryReadK(out int k)) return;
            var result = await _recognitionService!.RunBenchmarkAsync(k);
            AppendLog($"Benchmark: accuracy={result.Accuracy:P2}, KDTree={result.KdTreeSearchTime.TotalMilliseconds:F2} ms, Linear={result.LinearSearchTime.TotalMilliseconds:F2} ms");
        }
        catch (Exception ex)
        {
            AppendLog($"ERROR: {ex.Message}");
        }
    }

    private async void GenerateDemoSamplesButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (!EnsureConnected()) return;
            string baseDir = Path.Combine(AppContext.BaseDirectory, "DemoSamples");
            Directory.CreateDirectory(baseDir);

            var specs = new List<(string ClassName, Action<string, int> DrawAction)>
            {
                ("digit_0", (path, idx) => DrawDigit(path, "0", idx)),
                ("digit_5", (path, idx) => DrawDigit(path, "5", idx)),
                ("digit_8", (path, idx) => DrawDigit(path, "8", idx)),
                ("shape_circle", (path, idx) => DrawCircle(path, idx)),
                ("shape_square", (path, idx) => DrawSquare(path, idx)),
                ("shape_triangle", (path, idx) => DrawTriangle(path, idx))
            };

            int totalImported = 0;
            int perClass = 6;

            foreach (var spec in specs)
            {
                string classDir = Path.Combine(baseDir, spec.ClassName);
                Directory.CreateDirectory(classDir);

                for (int i = 0; i < perClass; i++)
                {
                    string filePath = Path.Combine(classDir, $"{spec.ClassName}_{i + 1}.png");
                    spec.DrawAction(filePath, i);
                    await _recognitionService!.AddTrainingImageAsync(filePath, spec.ClassName);
                    totalImported++;
                }
            }

            AppendLog($"Demo dataset generated and imported. Samples={totalImported}, folder={baseDir}");
            await RefreshDatabaseStatsAsync();
        }
        catch (Exception ex)
        {
            AppendLog($"ERROR: {ex.Message}");
        }
    }

    private bool TryReadK(out int k)
    {
        if (!int.TryParse(KTextBox.Text, out k) || k <= 0)
        {
            AppendLog("ERROR: k must be a positive integer.");
            return false;
        }

        return true;
    }

    private bool TryValidateImagePath(string? pathText, out string filePath)
    {
        filePath = pathText?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(filePath))
        {
            AppendLog("ERROR: Select image file first.");
            return false;
        }

        if (!File.Exists(filePath))
        {
            AppendLog($"ERROR: File not found: {filePath}");
            return false;
        }

        return true;
    }

    private async Task RefreshDatabaseStatsAsync()
    {
        if (!EnsureConnected()) return;
        var stats = await _recognitionService!.GetDatabaseOverviewAsync();
        DatabaseStatsTextBlock.Text =
            $"Статистика БД: classes={stats.Classes}, images={stats.Images}, features={stats.Features}, models={stats.Models}, experiments={stats.Experiments}, predictions={stats.Predictions}";
    }

    private async void CheckDatabaseButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (!EnsureConnected()) return;
            await RefreshDatabaseStatsAsync();
            AppendLog("Проверка БД выполнена: статистика обновлена.");
        }
        catch (Exception ex)
        {
            AppendLog($"ERROR: {ex.Message}");
        }
    }

    private static void DrawDigit(string path, string digit, int variationIndex)
    {
        using var bmp = CreateCanvas();
        using var g = Graphics.FromImage(bmp);
        ConfigureGraphics(g);
        g.Clear(Color.White);

        using var font = new Font("Arial", 76, System.Drawing.FontStyle.Bold, GraphicsUnit.Pixel);
        using var brush = new SolidBrush(Color.Black);
        var format = new StringFormat
        {
            Alignment = StringAlignment.Center,
            LineAlignment = StringAlignment.Center
        };

        float offsetX = (variationIndex % 3 - 1) * 4f;
        float offsetY = (variationIndex / 3 - 1) * 4f;
        var rect = new RectangleF(offsetX, offsetY, 128, 128);
        g.DrawString(digit, font, brush, rect, format);

        SavePng(path, bmp);
    }

    private static void DrawCircle(string path, int variationIndex)
    {
        using var bmp = CreateCanvas();
        using var g = Graphics.FromImage(bmp);
        ConfigureGraphics(g);
        g.Clear(Color.White);
        using var pen = new Pen(Color.Black, 10);
        int size = 70 + (variationIndex % 3) * 8;
        int left = (128 - size) / 2 + (variationIndex % 2 == 0 ? -3 : 3);
        int top = (128 - size) / 2;
        g.DrawEllipse(pen, left, top, size, size);
        SavePng(path, bmp);
    }

    private static void DrawSquare(string path, int variationIndex)
    {
        using var bmp = CreateCanvas();
        using var g = Graphics.FromImage(bmp);
        ConfigureGraphics(g);
        g.Clear(Color.White);
        using var pen = new Pen(Color.Black, 10);
        int size = 68 + (variationIndex % 3) * 8;
        int left = (128 - size) / 2 + (variationIndex % 2 == 0 ? -2 : 2);
        int top = (128 - size) / 2;
        g.DrawRectangle(pen, left, top, size, size);
        SavePng(path, bmp);
    }

    private static void DrawTriangle(string path, int variationIndex)
    {
        using var bmp = CreateCanvas();
        using var g = Graphics.FromImage(bmp);
        ConfigureGraphics(g);
        g.Clear(Color.White);
        using var pen = new Pen(Color.Black, 10);

        int shift = (variationIndex % 3 - 1) * 3;
        var p1 = new System.Drawing.Point(64 + shift, 24);
        var p2 = new System.Drawing.Point(28 + shift, 100);
        var p3 = new System.Drawing.Point(100 + shift, 100);
        g.DrawPolygon(pen, new[] { p1, p2, p3 });
        SavePng(path, bmp);
    }

    private static Bitmap CreateCanvas() => new Bitmap(128, 128);

    private static void ConfigureGraphics(Graphics g)
    {
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
        g.PixelOffsetMode = PixelOffsetMode.HighQuality;
    }

    private static void SavePng(string path, Bitmap bitmap)
    {
        bitmap.Save(path, ImageFormat.Png);
    }

    private void HelpButton_Click(object sender, RoutedEventArgs e)
    {
        const string helpText =
            "Справка по работе с приложением\n\n" +
            "1) Введите параметры подключения к уже существующей БД PostgreSQL.\n" +
            "2) Нажмите «Подключиться к БД».\n" +
            "3) При необходимости нажмите «Инициализировать таблицы».\n" +
            "4) Добавьте обучающие данные:\n" +
            "   - вручную (Class name + Image path + Добавить обучающее изображение)\n" +
            "   - либо кнопкой «Generate Demo Samples and Import».\n" +
            "5) Задайте k и нажмите «Обучить модель».\n" +
            "6) Выберите изображение и выполните классификацию (KDTree или линейно).\n" +
            "7) Для анализа скорости запустите benchmark.\n\n" +
            "Типичные ошибки:\n" +
            "- «Сначала подключитесь к БД» — не выполнено подключение.\n" +
            "- «No training samples found» — не добавлены обучающие изображения.\n" +
            "- «Select image file first» — не выбран файл.";

        MessageBox.Show(helpText, "Справка", MessageBoxButton.OK, MessageBoxImage.Information);
    }
}