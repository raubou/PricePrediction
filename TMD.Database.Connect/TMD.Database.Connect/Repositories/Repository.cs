using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using TMDInvestments.Models;

namespace TMD.Database.Connect.Repositories
{
    public sealed class Repository
    {

        //private SqlConnection _con;
        private static SqlCommand _com;
        private SqlDataAdapter _adaptor;
        //private static string _conStr;
        private static ILogger _log;
        private static SqlConnection _con;
        //public Repository(string conStr, ILogger log)
        //{
        //    _conStr = conStr;
        //    _log = log;
        //}

        public Repository(SqlConnection con, ILogger log)
        {
            _con = con;
            _log = log;
        }

        public DataSet GetData(string command)
        {
            //SqlConnection _con = new SqlConnection(_conStr);            
            CreateAdaptor(_con, command);
            DataSet reader = new DataSet();
            //while(_con.State != ConnectionState.Open)
            //{
            //    //wait
            //}
            if (_con.State != ConnectionState.Open)
            {
                try
                {
                    _con.Open();
                }
                catch (Exception ex3)
                {

                }
            }
            while(_con.State == ConnectionState.Connecting)
            {
                //wait
            }
            //Thread.Sleep(3000);
            try
            {
                if (_adaptor == null)
                    throw new Exception("Connection failed");
                _adaptor.Fill(reader, "Results");
                if (_con.State == ConnectionState.Open) { _con.Close(); _con.Dispose(); SqlConnection.ClearPool(_con); }
            }
            catch (Exception ex)
            {
                //Console.WriteLine($"An error has occured {ex.Source} - {ex.StackTrace} - {ex.InnerException?.Message} - {ex.Message}");
                //if (_con.State == ConnectionState.Open) { _con.Close(); _con.Dispose(); SqlConnection.ClearPool(_con); }
                _log?.LogError(ex.Source + " : " + ex.StackTrace + " : " + ex.InnerException?.Message + " : " + ex.Message);
            }
            ////finally { //if (_con.State == ConnectionState.Open) { _con.Close(); _con.Dispose(); SqlConnection.ClearPool(_con); } }
            return reader;
        }

        public DataSet GetData(string command, Dictionary<string, dynamic> param)
        {
            //SqlConnection _con = new SqlConnection(_conStr);
            CreateAdaptor(_con, command, param);
            DataSet reader = new DataSet();
            //while (_con.State != ConnectionState.Open)
            //{
            //    //wait
            //}
            if (_con.State != ConnectionState.Open)
            {
                try
                {
                    _con.Open();
                }
                catch (Exception ex3)
                {

                }
            }
            while (_con.State == ConnectionState.Connecting)
            {
                //wait
            }
            //Thread.Sleep(3000);
            try
            {
                if (_adaptor == null)
                    throw new Exception("Connection failed");
                _adaptor.Fill(reader, "Results");
                if (_con.State == ConnectionState.Open) { _con.Close(); SqlConnection.ClearPool(_con); }
            }
            catch (Exception ex)
            {
                //Console.WriteLine($"An error has occured {ex.Source} - {ex.StackTrace} - {ex.InnerException?.Message} - {ex.Message}");
                //foreach (var item in param)
                //{
                    //Console.WriteLine($"{item.Key} - {item.Value} {Environment.NewLine}");
                //}
                ////if (_con.State == ConnectionState.Open) { _con.Close(); _con.Dispose(); SqlConnection.ClearPool(_con); }
                _log?.LogError(ex.Source + " : " + ex.StackTrace + " : " + ex.InnerException?.Message + " : " + ex.Message);
            }
            ////finally { //if (_con.State == ConnectionState.Open) { _con.Close(); _con.Dispose(); SqlConnection.ClearPool(_con); } }
            return reader;
        }

        public DataSet GetData(string command, Dictionary<string, dynamic> param, ref SqlParameter output)
        {
            //SqlConnection _con = new SqlConnection(_conStr);
            CreateAdaptor(_con, command, param, ref output);
            DataSet reader = new DataSet();
            //while (_con.State != ConnectionState.Open)
            //{
            //    //wait
            //}
            if (_con.State != ConnectionState.Open)
            {
                try
                {
                    _con.Open();
                }
                catch (Exception ex3)
                {

                }
            }
            while (_con.State == ConnectionState.Connecting)
            {
                //wait
            }
            //Thread.Sleep(3000);
            try
            {
                if (_adaptor == null)
                    throw new Exception("Connection failed");
                _adaptor.Fill(reader, "Results");
                if (_con.State == ConnectionState.Open) { _con.Close(); SqlConnection.ClearPool(_con); }
            }
            catch (Exception ex)
            {
                //Console.WriteLine($"An error has occured {ex.Source} - {ex.StackTrace} - {ex.InnerException?.Message} - {ex.Message}");
                //foreach (var item in param)
                //{
                    //Console.WriteLine($"{item.Key} - {item.Value} {Environment.NewLine}");
                //}
                //if (_con.State == ConnectionState.Open) { _con.Close(); _con.Dispose(); SqlConnection.ClearPool(_con); }
                _log?.LogError(ex.Source + " : " + ex.StackTrace + " : " + ex.InnerException?.Message + " : " + ex.Message);
            }
            //finally { //if (_con.State == ConnectionState.Open) { _con.Close(); _con.Dispose(); SqlConnection.ClearPool(_con); } }
            return reader;
        }

        public static int? RunCommand(SqlConnection con, ILogger log, string command)
        {
            int results = 0;
            //SqlConnection _con = new SqlConnection(_conStr);
            //while (con.State != ConnectionState.Open)
            //{
            //    //wait
            //}
            if (con.State != ConnectionState.Open)
            {
                try
                {
                    con.Open();
                }
                catch (Exception ex3)
                {

                }
            }
            while (_con.State == ConnectionState.Connecting)
            {
                //wait
            }
            //Thread.Sleep(3000);
            try
            {
                CreateCommand(con, log, command, null);
                if (_com == null)
                    throw new Exception("Connection failed");
                results = _com.ExecuteNonQuery();
                if (_con.State == ConnectionState.Open) { _con.Close(); SqlConnection.ClearPool(_con); }
                results = 1;
            }
            catch (Exception ex)
            {
                //Console.WriteLine($"An error has occured {ex.Source} - {ex.StackTrace} - {ex.InnerException?.Message} - {ex.Message}");
                //if (param != null)
                //    foreach (var item in param)
                //    {
                //        //Console.WriteLine($"{item.Key} - {item.Value} {Environment.NewLine}");
                //    }
                //if (_con.State == ConnectionState.Open) { _con.Close(); _con.Dispose(); SqlConnection.ClearPool(_con); }
                log?.LogError(ex.Source + " : " + ex.StackTrace + " : " + ex.InnerException?.Message + " : " + ex.Message);
            }
            //finally { //if (_con.State == ConnectionState.Open) { _con.Close(); _con.Dispose(); SqlConnection.ClearPool(_con); } }
            return results;
        }

        public static int? PostData(SqlConnection con, ILogger log, string command, Dictionary<string, dynamic> param)
        {
            int results = 0;
            //SqlConnection _con = new SqlConnection(_conStr);
            //_con = new SqlConnection(_conStr);
            if (con.State != ConnectionState.Open)
            {
                try
                {
                    con.Open();
                }
                catch (Exception ex3)
                {

                }
            }
            while (_con.State == ConnectionState.Connecting)
            {
                //wait
            }
            //Thread.Sleep(3000);
            try
            {
                CreateCommand(con, log, command, param);
                if (_com == null)
                    throw new Exception("Connection failed");
                //string name = param["title"].ToString();
                log?.LogInformation("Post query " + command);
                results = _com.ExecuteNonQuery();
                if (_con.State == ConnectionState.Open) { _con.Close(); SqlConnection.ClearPool(_con); }
                //while (con.State != ConnectionState.Closed)
                //{
                //    //wait
                //}
                results = 1;
            }
            catch (Exception ex)
            {
                //Console.WriteLine($"An error has occured {ex.Source} - {ex.StackTrace} - {ex.InnerException?.Message} - {ex.Message}");
                //if(param != null)
                //    foreach(var item in param)
                //    {
                //        //Console.WriteLine($"{item.Key} - {item.Value} {Environment.NewLine}");
                //    }
                //if (_con.State == ConnectionState.Open) { _con.Close(); _con.Dispose(); SqlConnection.ClearPool(_con); }
                log?.LogError(ex.Source + " : " + ex.StackTrace + " : " + ex.InnerException?.Message + " : " + ex.Message);
            }
            //finally { //if (_con.State == ConnectionState.Open) { _con.Close(); _con.Dispose(); SqlConnection.ClearPool(_con); } }
            return results;
        }

        private static void CreateCommand(SqlConnection _con, ILogger _log, string command, Dictionary<string, dynamic> param)
        {
            //Retry connection errors three time
            try
            {
                //_con.Open();
                _com = new SqlCommand(command, _con);
                _com.CommandTimeout = 10000;
                _com.CommandType = System.Data.CommandType.StoredProcedure;
                if (param != null && param.Count() > 0)
                    _com.Parameters.AddRange(param.Select(x => new SqlParameter(x.Key, x.Value)).ToArray());
            }
            catch (Exception ex)
            {
                //if (_con.State == ConnectionState.Open) { _con.Close(); _con.Dispose(); SqlConnection.ClearPool(_con); }
                //try
                //{
                //    //_con.Open();
                //    _com = new SqlCommand(command, _con);
                //    _com.CommandTimeout = 10000;
                //    _com.CommandType = System.Data.CommandType.StoredProcedure;
                //    if (param != null && param.Count() > 0)
                //        _com.Parameters.AddRange(param.Select(x => new SqlParameter(x.Key, x.Value)).ToArray());
                //}
                //catch (Exception ex2)
                //{
                //    //if (_con.State == ConnectionState.Open) { _con.Close(); _con.Dispose(); SqlConnection.ClearPool(_con); }
                //    //try
                //    //{
                //    //    //_con.Open();
                //    //    _com = new SqlCommand(command, _con);
                //    //    _com.CommandTimeout = 10000;
                //    //    _com.CommandType = System.Data.CommandType.StoredProcedure;
                //    //    if (param != null && param.Count() > 0)
                //    //        _com.Parameters.AddRange(param.Select(x => new SqlParameter(x.Key, x.Value)).ToArray());
                //    //}
                //    //catch (Exception ex3)
                //    //{
                //    //    //if (_con.State == ConnectionState.Open) { _con.Close(); _con.Dispose(); SqlConnection.ClearPool(_con); }
                //    //    _log?.LogError(ex3.Source + " : " + ex3.StackTrace + " : " + ex3.InnerException?.Message + " : " + ex3.Message);
                //    //}
                //    _log?.LogError(ex2.Source + " : " + ex2.StackTrace + " : " + ex2.InnerException?.Message + " : " + ex2.Message);
                //}
                _log?.LogError(ex.Source + " : " + ex.StackTrace + " : " + ex.InnerException?.Message + " : " + ex.Message);
            }
        }

        private void CreateAdaptor(SqlConnection _con, string command)
        {
            //Retry connection errors three time
            try
            {
                //_con.Open();
                _adaptor = new SqlDataAdapter(command, _con);
                _adaptor.SelectCommand.CommandTimeout = 10000;
                _adaptor.SelectCommand.CommandType = System.Data.CommandType.StoredProcedure;
            }
            catch (Exception ex)
            {
                //if (_con.State == ConnectionState.Open) { _con.Close(); _con.Dispose(); SqlConnection.ClearPool(_con); }
                //try
                //{
                //    //_con.Open();
                //    _adaptor = new SqlDataAdapter(command, _con);
                //    _adaptor.SelectCommand.CommandTimeout = 10000;
                //    _adaptor.SelectCommand.CommandType = System.Data.CommandType.StoredProcedure;
                //}
                //catch (Exception ex2)
                //{
                //    //if (_con.State == ConnectionState.Open) { _con.Close(); _con.Dispose(); SqlConnection.ClearPool(_con); }
                //    try
                //    {
                //        //_con.Open();
                //        _adaptor = new SqlDataAdapter(command, _con);
                //        _adaptor.SelectCommand.CommandTimeout = 10000;
                //        _adaptor.SelectCommand.CommandType = System.Data.CommandType.StoredProcedure;
                //    }
                //    catch (Exception ex3)
                //    {
                //        //if (_con.State == ConnectionState.Open) { _con.Close(); _con.Dispose(); SqlConnection.ClearPool(_con); }
                //        _log?.LogError(ex3.Source + " : " + ex3.StackTrace + " : " + ex3.InnerException?.Message + " : " + ex3.Message);
                //    }
                //    _log?.LogError(ex2.Source + " : " + ex2.StackTrace + " : " + ex2.InnerException?.Message + " : " + ex2.Message);
                //}
                _log?.LogError(ex.Source + " : " + ex.StackTrace + " : " + ex.InnerException?.Message + " : " + ex.Message);
            }
        }

        private void CreateAdaptor(SqlConnection _con, string command, Dictionary<string, dynamic> parm)
        {
            //Retry connection errors three time
            try
            {
                //_con.Open();
                _adaptor = new SqlDataAdapter(command, _con);
                _adaptor.SelectCommand.CommandTimeout = 10000;
                _adaptor.SelectCommand.CommandType = System.Data.CommandType.StoredProcedure;
                if (parm != null && parm.Count() > 0)
                    _adaptor.SelectCommand.Parameters.AddRange(parm.Select(x => new SqlParameter(x.Key, x.Value)).ToArray());
            }
            catch (Exception ex)
            {
                //if (_con.State == ConnectionState.Open) { _con.Close(); _con.Dispose(); SqlConnection.ClearPool(_con); }
                //try
                //{
                //    //_con.Open();
                //    _adaptor = new SqlDataAdapter(command, _con);
                //    _adaptor.SelectCommand.CommandTimeout = 10000;
                //    _adaptor.SelectCommand.CommandType = System.Data.CommandType.StoredProcedure;
                //    if (parm != null && parm.Count() > 0)
                //        _adaptor.SelectCommand.Parameters.AddRange(parm.Select(x => new SqlParameter(x.Key, x.Value)).ToArray());
                //}
                //catch (Exception ex2)
                //{
                //    //if (_con.State == ConnectionState.Open) { _con.Close(); _con.Dispose(); SqlConnection.ClearPool(_con); }
                //    try
                //    {
                //        //_con.Open();
                //        _adaptor = new SqlDataAdapter(command, _con);
                //        _adaptor.SelectCommand.CommandTimeout = 10000;
                //        _adaptor.SelectCommand.CommandType = System.Data.CommandType.StoredProcedure;
                //        if (parm != null && parm.Count() > 0)
                //            _adaptor.SelectCommand.Parameters.AddRange(parm.Select(x => new SqlParameter(x.Key, x.Value)).ToArray());
                //    }
                //    catch (Exception ex3)
                //    {
                //        //if (_con.State == ConnectionState.Open) { _con.Close(); _con.Dispose(); SqlConnection.ClearPool(_con); }
                //        _log?.LogError(ex3.Source + " : " + ex3.StackTrace + " : " + ex3.InnerException?.Message + " : " + ex3.Message);
                //    }
                //    _log?.LogError(ex2.Source + " : " + ex2.StackTrace + " : " + ex2.InnerException?.Message + " : " + ex2.Message);
                //}
                _log?.LogError(ex.Source + " : " + ex.StackTrace + " : " + ex.InnerException?.Message + " : " + ex.Message);
            }
        }

        private void CreateAdaptor(SqlConnection _con, string command, Dictionary<string, dynamic> parm, ref SqlParameter output)
        {
            //Retry connection errors three time
            try
            {
                //_con.Open();
                _adaptor = new SqlDataAdapter(command, _con);
                _adaptor.SelectCommand.CommandTimeout = 10000;
                _adaptor.SelectCommand.CommandType = System.Data.CommandType.StoredProcedure;
                if (parm != null && parm.Count() > 0)
                    _adaptor.SelectCommand.Parameters.AddRange(parm.Select(x => new SqlParameter(x.Key, x.Value)).ToArray());
                if (output != null)
                    _adaptor.SelectCommand.Parameters.Add(output);
            }
            catch (Exception ex)
            {
                _log?.LogError(ex.Source + " : " + ex.StackTrace + " : " + ex.InnerException?.Message + " : " + ex.Message);
            }
        }

        public static void BulkInserter(string tableName, SqlConnection con, ILogger log, DataRow[] dataRows)
        {
            var startTime = DateTime.Now;
            if (con.State != ConnectionState.Open)
            {
                try
                {
                    con.Open();
                }
                catch (Exception ex3)
                {

                }
            }
            while (con.State == ConnectionState.Connecting)
            {
                //wait
            }
            var transaction = con.BeginTransaction();
            try
            {
                using (var sbCopy = new SqlBulkCopy(con, SqlBulkCopyOptions.Default, transaction))
                {
                    sbCopy.BulkCopyTimeout = 0;
                    sbCopy.BatchSize = 300000;
                    sbCopy.DestinationTableName = tableName;
                    foreach(DataColumn item in dataRows[0].Table.Columns)
                    {
                        sbCopy.ColumnMappings.Add(item.ColumnName, item.ColumnName);
                    }
                    
                    sbCopy.WriteToServer(dataRows);
                }
                transaction.Commit();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                transaction.Rollback();
            }
            finally
            {
                transaction.Dispose();
                con.Close();
                var endTime = DateTime.Now;
                Console.WriteLine("Upload time elapsed: {0} seconds", (endTime - startTime).TotalSeconds);
            }
        }

        public static void BulkInserter(string tableName, SqlConnection con, ILogger log, List<RssModelItems> rss)
        {
            var startTime = DateTime.Now;
            if (con.State != ConnectionState.Open)
            {
                try
                {
                    con.Open();
                }
                catch (Exception ex3)
                {

                }
            }
            while (con.State == ConnectionState.Connecting)
            {
                //wait
            }
            var transaction = con.BeginTransaction();
            try
            {
                //var connStr = connection.ConnectionString;
                using (var sbCopy = new SqlBulkCopy(con, SqlBulkCopyOptions.Default, transaction))
                {
                    sbCopy.BulkCopyTimeout = 0;
                    sbCopy.BatchSize = 300000;
                    sbCopy.DestinationTableName = tableName;
                    var reader = rss.CreateDataTableItem();
                    sbCopy.WriteToServer(reader);
                }
                transaction.Commit();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                transaction.Rollback();
            }
            finally
            {
                transaction.Dispose();
                con.Close();
                var endTime = DateTime.Now;
                Console.WriteLine("Upload time elapsed: {0} seconds", (endTime - startTime).TotalSeconds);
            }
        }
    }
}
