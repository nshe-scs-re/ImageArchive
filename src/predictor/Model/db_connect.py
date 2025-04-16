import pymssql

def connect_to_database():
    try:
        conn = pymssql.connect(
            server='',
            user='',
            password='',
            database='',
            port=
        )
        print("Connected to SQL Server")
        return conn
    except Exception as e:
        print(f"Database connection failed: {e}")
        return None
