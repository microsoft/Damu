.\scripts\loadenv.ps1

start http://127.0.0.1:50505
.\.venv\Scripts\python.exe -m uvicorn app:app  --port 50505 --reload
