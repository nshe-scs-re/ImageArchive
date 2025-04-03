import pymssql

def connect_to_database():
    try:
        connection = pymssql.connect(
            server='127.0.0.1',
            user='sa',
            password='GZLCS!^S(kx9',
            database='master',
            port=1433
        )
        print("Connected to SQL Server")
        return connection
    except Exception as e:
        print(f"Connection failed: {e}")
        return None

# Optional test
if __name__ == "__main__":
    conn = connect_to_database()
    if conn:
        cursor = conn.cursor()
        cursor.execute("SELECT @@VERSION")
        print(f"SQL Server version: {cursor.fetchone()[0]}")
        conn.close()
