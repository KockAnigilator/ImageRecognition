using ImageRecognition.Application.Interfaces;
using ImageRecognition.Application.Services;
using ImageRecognition.Infrastructure.Database;
using Microsoft.Win32;
using System.Windows;

namespace ImageRecognition.UI;

public partial class MainWindow : Window
{
    private readonly IRecognitionService _recognitionService;

    public MainWindow()
    {
        InitializeComponent();

        _recognitionService = ApplicationFactory.CreateDefaultRecognitionService(new PostgresOptions
        {
            Host = "localhost",
            Port = 5432,
            Database = "Image_recognition_db",
            Username = "postgres",
            Password = "10112275"
        });
    }

    private void AppendLog(string message)
    {
        LogTextBox.AppendText($"{DateTime.Now:HH:mm:ss} | {message}{Environment.NewLine}");
        LogTextBox.ScrollToEnd();
    }

    private async void InitializeDatabaseButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            await _recognitionService.InitializeDatabaseAsync();
            AppendLog("Database initialized (tables created if not exists).");
        }
        catch (Exception ex)
        {
            AppendLog($"ERROR: {ex.Message}");
        }
    }

    private void BrowseImageButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Filter = "Image files|*.png;*.jpg;*.jpeg;*.bmp;*.gif|All files|*.*"
        };

        if (dialog.ShowDialog() == true)
        {
            ImagePathTextBox.Text = dialog.FileName;
        }
    }

    private async void AddTrainingImageButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            int imageId = await _recognitionService.AddTrainingImageAsync(ImagePathTextBox.Text, ClassNameTextBox.Text);
            AppendLog($"Training image saved. ImageId={imageId}, class={ClassNameTextBox.Text}");
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
            int k = int.Parse(KTextBox.Text);
            var result = await _recognitionService.TrainAsync(ModelNameTextBox.Text, k);
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
            int k = int.Parse(KTextBox.Text);
            var result = await _recognitionService.ClassifyImageAsync(ImagePathTextBox.Text, k, useKdTree);
            AppendLog($"Classified ({(useKdTree ? "KDTree" : "Linear")}): classId={result.PredictedClassId}, search={result.SearchTime.TotalMilliseconds:F2} ms");
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
            int k = int.Parse(KTextBox.Text);
            var result = await _recognitionService.RunBenchmarkAsync(k);
            AppendLog($"Benchmark: accuracy={result.Accuracy:P2}, KDTree={result.KdTreeSearchTime.TotalMilliseconds:F2} ms, Linear={result.LinearSearchTime.TotalMilliseconds:F2} ms");
        }
        catch (Exception ex)
        {
            AppendLog($"ERROR: {ex.Message}");
        }
    }
}