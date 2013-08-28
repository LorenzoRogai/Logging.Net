using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using MySql.Data.MySqlClient;
using System.Threading;

namespace LoggingNet
{
    public class DatabaseManager
    {
        public Boolean[] AvailableClients;

        public Thread ClientMonitor;
        public int ClientStarvation;
        public DatabaseClient[] Clients;
        public Database Database;
        public DatabaseServer Server;

        public DatabaseManager(DatabaseServer _Server, Database _Database)
        {
            Server = _Server;
            Database = _Database;

            Clients = new DatabaseClient[0];
            AvailableClients = new Boolean[0];
            ClientStarvation = 0;

            StartClientMonitor();
        }

        public string ConnectionString
        {
            get
            {
                var ConnString = new MySqlConnectionStringBuilder();

                ConnString.Server = Server.Hostname;
                ConnString.Port = Server.Port;
                ConnString.UserID = Server.Username;
                ConnString.Password = Server.Password;
                ConnString.Database = Database.DatabaseName;
                ConnString.MinimumPoolSize = Database.PoolMinSize;
                ConnString.MaximumPoolSize = Database.PoolMaxSize;

                return ConnString.ToString();
            }
        }

        public void DestroyClients()
        {
            lock (Clients)
            {
                for (int i = 0; i < Clients.Length; i++)
                {
                    Clients[i].Destroy();
                    Clients[i] = null;
                }
            }
        }

        public void DestroyDatabaseManager()
        {
            StopClientMonitor();

            lock (Clients)
            {
                for (int i = 0; i < Clients.Length; i++)
                {
                    try
                    {
                        Clients[i].Destroy();
                        Clients[i] = null;
                    }
                    catch (NullReferenceException)
                    {
                    }
                }
            }

            Server = null;
            Database = null;
            Clients = null;
            AvailableClients = null;
        }

        public void StartClientMonitor()
        {
            if (ClientMonitor != null)
            {
                return;
            }

            ClientMonitor = new Thread(MonitorClients);
            ClientMonitor.Name = "DB Monitor";
            ClientMonitor.Priority = ThreadPriority.Lowest;
            ClientMonitor.Start();
        }

        public void StopClientMonitor()
        {
            if (ClientMonitor == null)
            {
                return;
            }

            try
            {
                ClientMonitor.Abort();
            }

            catch (ThreadAbortException)
            {
            }

            ClientMonitor = null;
        }

        public void MonitorClients()
        {
            while (true)
            {
                try
                {
                    lock (Clients)
                    {
                        DateTime DT = DateTime.Now;

                        for (int i = 0; i < Clients.Length; i++)
                        {
                            if (Clients[i].State != ConnectionState.Closed)
                            {
                                if (Clients[i].InactiveTime >= 60) // Not used in the last %x% seconds
                                {
                                    Clients[i].Disconnect();
                                }
                            }
                        }
                    }

                    Thread.Sleep(10000);
                }

                catch (ThreadAbortException)
                {
                }

                catch (Exception e)
                {
                    throw new Exception("An error occured in database manager client monitor: " + e.Message);
                }
            }
        }

        public DatabaseClient GetClient()
        {
            lock (Clients)
            {
                lock (AvailableClients)
                {
                    for (uint i = 0; i < Clients.Length; i++)
                    {
                        if (AvailableClients[i])
                        {
                            ClientStarvation = 0;

                            if (Clients[i].State == ConnectionState.Closed)
                            {
                                try
                                {
                                    Clients[i].Connect();
                                }

                                catch (Exception e)
                                {
                                    throw new Exception("Could not get database client: " + e.Message);
                                }
                            }

                            if (Clients[i].State == ConnectionState.Open)
                            {
                                AvailableClients[i] = false;

                                Clients[i].UpdateLastActivity();
                                return Clients[i];
                            }
                        }
                    }
                }

                ClientStarvation++;

                if (ClientStarvation >= ((Clients.Length + 1) / 2))
                {
                    ClientStarvation = 0;
                    SetClientAmount((uint)(Clients.Length + 1 * 1.3f));
                    return GetClient();
                }

                var Anonymous = new DatabaseClient(0, this);
                Anonymous.Connect();

                return Anonymous;
            }
        }

        public void SetClientAmount(uint Amount)
        {
            lock (Clients)
            {
                lock (AvailableClients)
                {
                    if (Clients.Length == Amount)
                    {
                        return;
                    }

                    if (Amount < Clients.Length)
                    {
                        for (uint i = Amount; i < Clients.Length; i++)
                        {
                            Clients[i].Destroy();
                            Clients[i] = null;
                        }
                    }

                    var _Clients = new DatabaseClient[Amount];
                    var _AvailableClients = new bool[Amount];

                    for (uint i = 0; i < Amount; i++)
                    {
                        if (i < Clients.Length)
                        {
                            _Clients[i] = Clients[i];
                            _AvailableClients[i] = AvailableClients[i];
                        }
                        else
                        {
                            _Clients[i] = new DatabaseClient((i + 1), this);
                            _AvailableClients[i] = true;
                        }
                    }

                    Clients = _Clients;
                    AvailableClients = _AvailableClients;
                }
            }
        }

        public void ReleaseClient(uint Handle)
        {
            lock (Clients)
            {
                lock (AvailableClients)
                {
                    if (Clients.Length >= (Handle - 1)) // Ensure client exists
                    {
                        AvailableClients[Handle - 1] = true;
                    }
                }
            }
        }
    }

    public class Database
    {
        public string DatabaseName;
        public uint PoolMaxSize;
        public uint PoolMinSize;

        public Database(string _DatabaseName, uint _PoolMinSize, uint _PoolMaxSize)
        {
            DatabaseName = _DatabaseName;

            PoolMinSize = _PoolMinSize;
            PoolMaxSize = _PoolMaxSize;
        }
    }

    public class DatabaseServer
    {
        public string Hostname;
        public string Password;
        public uint Port;

        public string Username;

        public DatabaseServer(string _Hostname, uint _Port, string _Username, string _Password)
        {
            Hostname = _Hostname;
            Port = _Port;
            Username = _Username;
            Password = _Password;
        }
    }

    public class DatabaseClient : IDisposable
    {
        public MySqlCommand Command;
        public MySqlConnection Connection;
        public uint Handle;

        public DateTime LastActivity;

        public DatabaseManager Manager;

        public DatabaseClient(uint _Handle, DatabaseManager _Manager)
        {
            if (_Manager == null)
            {
                throw new ArgumentNullException("[DBClient.Connect]: Invalid database handle");
            }

            Handle = _Handle;
            Manager = _Manager;

            Connection = new MySqlConnection(Manager.ConnectionString);
            Command = Connection.CreateCommand();

            UpdateLastActivity();
        }

        public Boolean IsAnonymous
        {
            get { return (Handle == 0); }
        }

        public int InactiveTime
        {
            get { return (int)(DateTime.Now - LastActivity).TotalSeconds; }
        }

        public ConnectionState State
        {
            get { return (Connection != null) ? Connection.State : ConnectionState.Broken; }
        }

        public void Dispose()
        {
            if (IsAnonymous)
            {
                Destroy();
                return;
            }

            Command.CommandText = null;
            Command.Parameters.Clear();

            Manager.ReleaseClient(Handle);
        }

        public void Connect()
        {
            try
            {
                Connection.Open();
            }
            catch (MySqlException e)
            {
                throw new Exception("Could not open MySQL Connection - " + e.Message);
            }
        }

        public void Disconnect()
        {
            try
            {
                Connection.Close();
            }
            catch
            {
            }
        }

        public void Destroy()
        {
            Disconnect();

            Connection.Dispose();
            Connection = null;

            Command.Dispose();
            Command = null;

            Manager = null;
        }

        public void UpdateLastActivity()
        {
            LastActivity = DateTime.Now;
        }

        public void AddParamWithValue(string sParam, object val)
        {
            Command.Parameters.AddWithValue(sParam, val);
        }

        public void ExecuteQuery(string sQuery)
        {
            Command.CommandText = sQuery;
            Command.ExecuteScalar();
            Command.CommandText = null;
        }

        public DataSet ReadDataSet(string Query)
        {
            var DataSet = new DataSet();
            Command.CommandText = Query;

            using (var Adapter = new MySqlDataAdapter(Command))
            {
                Adapter.Fill(DataSet);
            }

            Command.CommandText = null;
            return DataSet;
        }

        public DataTable ReadDataTable(string Query)
        {
            var DataTable = new DataTable();
            Command.CommandText = Query;

            using (var Adapter = new MySqlDataAdapter(Command))
            {
                Adapter.Fill(DataTable);
            }

            Command.CommandText = null;
            return DataTable;
        }

        public DataRow ReadDataRow(string Query)
        {
            DataTable DataTable = ReadDataTable(Query);

            if (DataTable != null && DataTable.Rows.Count > 0)
            {
                return DataTable.Rows[0];
            }

            return null;
        }

        public string ReadString(string Query)
        {
            Command.CommandText = Query;
            string result = Command.ExecuteScalar().ToString();
            Command.CommandText = null;
            return result;
        }

        public Int32 ReadInt32(string Query)
        {
            Command.CommandText = Query;
            Int32 result = Int32.Parse(Command.ExecuteScalar().ToString());
            Command.CommandText = null;
            return result;
        }
    }

    public class DatabaseException : Exception
    {
        public DatabaseException(string sMessage)
            : base(sMessage)
        {
        }
    }
}
