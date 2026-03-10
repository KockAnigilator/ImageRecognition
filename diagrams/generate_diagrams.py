#!/usr/bin/env python
# -*- coding: utf-8 -*-
"""
Генерация диаграмм для курсовой работы (Use Case, ER, Class).
Создаёт файлы в форматах PlantUML (.puml) и Mermaid (.mmd).

Просмотр:
  - PlantUML: VS Code (расширение PlantUML) или https://www.plantuml.com/plantuml
  - Mermaid: VS Code (расширение Mermaid) или https://mermaid.live
"""

import os

DIAGRAMS_DIR = os.path.dirname(os.path.abspath(__file__))


def ensure_dir(path: str) -> None:
    os.makedirs(path, exist_ok=True)


def write_file(filename: str, content: str) -> str:
    filepath = os.path.join(DIAGRAMS_DIR, filename)
    with open(filepath, "w", encoding="utf-8") as f:
        f.write(content)
    return filepath


# ========== USE CASE ==========

USE_CASE_PUML = """@startuml use_case
left to right direction
skinparam backgroundColor white
skinparam defaultFontName Arial
skinparam defaultFontSize 12

actor "Пользователь" as user

rectangle "Система распознавания изображений" {
  usecase "Загрузить изображения" as UC1
  usecase "Обучить модель" as UC2
  usecase "Построить KD-Tree" as UC3
  usecase "Классифицировать изображение" as UC4
  usecase "Просмотреть результат" as UC5
  usecase "Посмотреть точность" as UC6
  usecase "Измерить время работы" as UC7
}

user --> UC1
user --> UC2
user --> UC3
user --> UC4
user --> UC5
user --> UC6
user --> UC7

UC2 ..> UC3 : включает
UC4 ..> UC5 : использует
@enduml
"""

USE_CASE_MMD = """flowchart LR
    subgraph Actor
        U[Пользователь]
    end

    subgraph System["Система распознавания"]
        UC1[Загрузить изображения]
        UC2[Обучить модель]
        UC3[Построить KD-Tree]
        UC4[Классифицировать изображение]
        UC5[Просмотреть результат]
        UC6[Посмотреть точность]
        UC7[Измерить время работы]
    end

    U --> UC1
    U --> UC2
    U --> UC3
    U --> UC4
    U --> UC5
    U --> UC6
    U --> UC7

    UC2 -.-> UC3
    UC4 -.-> UC5
"""


# ========== ER (Entity-Relationship) ==========

ER_PUML = """@startuml er_database
skinparam backgroundColor white
skinparam defaultFontName Arial
skinparam linetype ortho

entity "classes" {
  * id : INT <<PK>>
  --
  name : VARCHAR
  description : TEXT
  created_at : TIMESTAMP
}

entity "images" {
  * id : INT <<PK>>
  --
  class_id : INT <<FK>>
  file_path : VARCHAR
  created_at : TIMESTAMP
}

entity "features" {
  * id : INT <<PK>>
  --
  image_id : INT <<FK>>
  vector : FLOAT8[]
}

entity "models" {
  * id : INT <<PK>>
  --
  name : VARCHAR
  dimension : INT
  training_sample_count : INT
  default_k : INT
  created_at : TIMESTAMP
  description : TEXT
}

entity "experiments" {
  * id : INT <<PK>>
  --
  model_id : INT <<FK>>
  train_sample_count : INT
  test_sample_count : INT
  accuracy : DOUBLE
  kd_tree_build_time_ms : DOUBLE
  kd_tree_search_time_ms : DOUBLE
  linear_search_time_ms : DOUBLE
  performed_at : TIMESTAMP
  notes : TEXT
}

entity "predictions" {
  * id : INT <<PK>>
  --
  image_id : INT <<FK>>
  model_id : INT <<FK>>
  predicted_class_id : INT
  actual_class_id : INT
  distance : DOUBLE
  used_kd_tree : BOOLEAN
  k : INT
  created_at : TIMESTAMP
}

classes ||--o{ images : "1:N"
images ||--o| features : "1:1"
models ||--o{ experiments : "1:N"
models ||--o{ predictions : "1:N"
images ||--o{ predictions : "1:N"

@enduml
"""

ER_MMD = """erDiagram
    classes {
        int id PK
        varchar name
        text description
        timestamp created_at
    }

    images {
        int id PK
        int class_id FK
        varchar file_path
        timestamp created_at
    }

    features {
        int id PK
        int image_id FK
        string vector
    }

    models {
        int id PK
        varchar name
        int dimension
        int training_sample_count
        int default_k
        timestamp created_at
        text description
    }

    experiments {
        int id PK
        int model_id FK
        int train_sample_count
        int test_sample_count
        double accuracy
        double kd_tree_build_time_ms
        double kd_tree_search_time_ms
        double linear_search_time_ms
        timestamp performed_at
        text notes
    }

    predictions {
        int id PK
        int image_id FK
        int model_id FK
        int predicted_class_id
        int actual_class_id
        double distance
        boolean used_kd_tree
        int k
        timestamp created_at
    }

    classes ||--o{ images : "имеет"
    images ||--o| features : "признаки"
    models ||--o{ experiments : "эксперименты"
    models ||--o{ predictions : "предсказания"
    images ||--o{ predictions : "результаты"
"""


# ========== CLASS DIAGRAM ==========

CLASS_PUML = """@startuml class_diagram
skinparam backgroundColor white
skinparam defaultFontName Arial

package "Domain.Algorithms" {
  class KDNode {
    + Point : double[]
    + Label : int
    + Left : KDNode
    + Right : KDNode
    + Axis : int
  }

  class KDTree {
    + Root : KDNode
    + Dimension : int
    + BuildTree(points, labels)
    + Insert(point, label)
    + NearestNeighbor(target)
    + KNearestNeighbors(target, k)
  }

  class KNearestNeighbors {
    + Classify(tree, featureVector, k)
    + ClassifyLinear(trainingPoints, trainingLabels, featureVector, k)
  }

  class DistanceCalculator {
    + EuclideanDistance(a, b)
  }

  KDTree *-- KDNode : содержит
  KNearestNeighbors --> KDTree : использует
  KNearestNeighbors --> DistanceCalculator : использует
}

package "Domain.Entities" {
  class ImageClass {
    + Id : int
    + Name : string
    + Description : string
  }

  class ImageRecord {
    + Id : int
    + ClassId : int
    + FilePath : string
  }

  class ImageFeatures {
    + Id : int
    + ImageId : int
    + Vector : double[]
  }

  class ModelInfo {
    + Id : int
    + Name : string
    + Dimension : int
    + DefaultK : int
  }

  class Experiment {
    + Id : int
    + ModelId : int
    + Accuracy : double
  }

  class Prediction {
    + Id : int
    + ImageId : int
    + PredictedClassId : int
    + UsedKdTree : bool
  }

  ImageClass "1" -- "N" ImageRecord
  ImageRecord "1" -- "0..1" ImageFeatures
  ModelInfo "1" -- "N" Experiment
  ModelInfo "1" -- "N" Prediction
  ImageRecord "1" -- "N" Prediction
}

package "Application" {
  interface IImagePreprocessingService {
    + ExtractFeatures(filePath) : double[]
  }

  class ImagePreprocessingService {
    + ExtractFeatures(filePath) : double[]
  }

  IImagePreprocessingService <|.. ImagePreprocessingService : implements
}

package "Infrastructure" {
  class PostgresOptions {
    + Host : string
    + Port : int
    + Database : string
  }

  class PostgresConnectionFactory {
    + CreateOpenConnection() : NpgsqlConnection
  }

  PostgresConnectionFactory --> PostgresOptions : использует
}

@enduml
"""

CLASS_MMD = """classDiagram
    class KDNode {
        +double[] Point
        +int Label
        +KDNode Left
        +KDNode Right
        +int Axis
    }

    class KDTree {
        +KDNode Root
        +int Dimension
        +BuildTree(points, labels)
        +Insert(point, label)
        +NearestNeighbor(target)
        +KNearestNeighbors(target, k)
    }

    class KNearestNeighbors {
        +Classify(tree, featureVector, k)
        +ClassifyLinear(trainingPoints, trainingLabels, featureVector, k)
    }

    class DistanceCalculator {
        +EuclideanDistance(a, b)
    }

    KDTree *-- KDNode
    KNearestNeighbors --> KDTree
    KNearestNeighbors --> DistanceCalculator

    class ImageClass {
        +int Id
        +string Name
    }

    class ImageRecord {
        +int Id
        +int ClassId
        +string FilePath
    }

    class ImageFeatures {
        +int Id
        +int ImageId
        +double[] Vector
    }

    class ModelInfo {
        +int Id
        +string Name
        +int Dimension
    }

    ImageClass "1" --> "N" ImageRecord
    ImageRecord "1" --> "0..1" ImageFeatures

    class IImagePreprocessingService {
        <<interface>>
        +ExtractFeatures(filePath)
    }

    class ImagePreprocessingService {
        +ExtractFeatures(filePath)
    }

    IImagePreprocessingService <|.. ImagePreprocessingService

    class PostgresOptions {
        +string Host
        +int Port
        +string Database
    }

    class PostgresConnectionFactory {
        +CreateOpenConnection()
    }
"""


# ========== COMPONENT / ARCHITECTURE ==========

COMPONENT_PUML = """@startuml component
skinparam backgroundColor white
skinparam defaultFontName Arial

package "ImageRecognition.UI" {
  [MainWindow] as UI
}

package "ImageRecognition.Application" {
  [ImagePreprocessingService] as App
}

package "ImageRecognition.Domain" {
  [KDTree] as KD
  [KNearestNeighbors] as KNN
  [DistanceCalculator] as DC
}

package "ImageRecognition.Infrastructure" {
  [PostgresConnectionFactory] as DB
  database "PostgreSQL" as PG
}

UI --> App : вызывает
App --> KD : использует
App --> KNN : использует
KNN --> KD : использует
KNN --> DC : использует
App --> DB : использует
DB --> PG : подключается

@enduml
"""

COMPONENT_MMD = """flowchart TB
    subgraph UI["ImageRecognition.UI"]
        MainWindow[MainWindow]
    end

    subgraph Application["ImageRecognition.Application"]
        Preprocessing[ImagePreprocessingService]
    end

    subgraph Domain["ImageRecognition.Domain"]
        KDTree[KDTree]
        KNN[KNearestNeighbors]
        DC[DistanceCalculator]
    end

    subgraph Infrastructure["ImageRecognition.Infrastructure"]
        DB[PostgresConnectionFactory]
    end

    PostgreSQL[(PostgreSQL)]

    MainWindow --> Preprocessing
    Preprocessing --> KDTree
    Preprocessing --> KNN
    KNN --> KDTree
    KNN --> DC
    Preprocessing --> DB
    DB --> PostgreSQL
"""


# ========== SEQUENCE (пример: классификация) ==========

SEQUENCE_PUML = """@startuml sequence
skinparam backgroundColor white
skinparam defaultFontName Arial

actor Пользователь
participant "UI (WPF)" as UI
participant "Application" as App
participant "ImagePreprocessing" as IP
participant "KNearestNeighbors" as KNN
participant "KDTree" as KD
participant "Database" as DB

Пользователь -> UI: Загрузить изображение
UI -> App: Классифицировать(filePath)
App -> IP: ExtractFeatures(filePath)
IP --> App: vector[256]

App -> DB: Получить модель
DB --> App: KDTree, метаданные

App -> KNN: Classify(tree, vector, k)
KNN -> KD: KNearestNeighbors(vector, k)
KD --> KNN: k ближайших соседей
KNN --> KNN: MajorityVote
KNN --> App: predictedClassId

App --> UI: Результат (класс, время)
UI --> Пользователь: Показать результат

@enduml
"""


def main() -> None:
    ensure_dir(DIAGRAMS_DIR)

    files = [
        ("use_case.puml", USE_CASE_PUML),
        ("use_case.mmd", USE_CASE_MMD),
        ("er_database.puml", ER_PUML),
        ("er_database.mmd", ER_MMD),
        ("class_diagram.puml", CLASS_PUML),
        ("class_diagram.mmd", CLASS_MMD),
        ("component.puml", COMPONENT_PUML),
        ("component.mmd", COMPONENT_MMD),
        ("sequence_classify.puml", SEQUENCE_PUML),
    ]

    for name, content in files:
        path = write_file(name, content)
        print(f"Создан: {path}")

    print("\nКак просмотреть:")
    print("  PlantUML (.puml): VS Code + PlantUML, или https://www.plantuml.com/plantuml")
    print("  Mermaid (.mmd):   VS Code + Mermaid, или https://mermaid.live")


if __name__ == "__main__":
    main()
