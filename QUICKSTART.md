# Быстрый старт

## Запуск системы

```bash
docker compose up --build
```

## Примеры использования API

### 1. Загрузка работы студента

```bash
curl -X POST "http://localhost:5000/api/filestorage/submit" \
  -F "file=@/path/to/work.txt" \
  -F "studentName=Иван Иванов" \
  -F "assignmentId=ASSIGNMENT-001"
```

Ответ:
```json
{
  "id": "guid-here",
  "studentName": "Иван Иванов",
  "assignmentId": "ASSIGNMENT-001",
  "submittedAt": "2024-12-10T10:00:00Z",
  "fileName": "work.txt",
  "filePath": "/app/uploads/...",
  "fileHash": "sha256-hash"
}
```

### 2. Получение информации о сдаче

```bash
curl "http://localhost:5000/api/filestorage/{workId}"
```

### 2.1. Скачивание файла работы

```bash
curl "http://localhost:5000/api/filestorage/{workId}/file" -o downloaded_work.txt
```

### 2.2. Получение всех сдач по заданию

```bash
curl "http://localhost:5000/api/filestorage/assignment/{assignmentId}"
```

### 3. Получение отчетов по работе

```bash
curl "http://localhost:5000/api/analysis/works/{workId}/reports"
```

Ответ:
```json
{
  "workId": "guid-here",
  "reports": [
    {
      "reportId": "guid-here",
      "status": "Completed",
      "hasPlagiarism": false,
      "createdAt": "2024-12-10T10:00:00Z"
    }
  ]
}
```

### 4. Получение облака слов

```bash
curl "http://localhost:5000/api/analysis/report/{reportId}/wordcloud"
```

Ответ:
```json
{
  "reportId": "guid-here",
  "wordCloudUrl": "https://quickchart.io/chart?c=..."
}
```

## Swagger UI

Доступна по адресам:
- API Gateway: http://localhost:5000/swagger
- File Storing Service: http://localhost:5001/swagger
- File Analysis Service: http://localhost:5002/swagger

## Остановка системы

```bash
docker compose down
```
