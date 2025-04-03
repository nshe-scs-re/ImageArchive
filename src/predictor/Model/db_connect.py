import pymssql

def connect_to_database():
    try:
        conn = pymssql.connect(
            server='127.0.0.1',
            user='sa',
            password='X1wdgbqjlTSU',
            database='ImageDb',
            port=1433
        )
        print("Connected to SQL Server")
        return conn
    except Exception as e:
        print(f"Database connection failed: {e}")
        return None
