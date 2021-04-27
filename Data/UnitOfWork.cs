using Core;
using Core.Repositories;
using Data.Repositories;
using Data.Repositories.MySql;
using NLog;

namespace Data
{
    public class UnitOfWork : IUnitOfWork
    {
        // private readonly SQLDB _context;
        private string _connectionString;
        private GCMRepository _gCMRepository;
        private Logger _logger { get; set; }
        public UnitOfWork(string connectionString, Logger logger) 
        {
            _connectionString = connectionString;
            _logger = logger;
        }

        public IGCMRepository GCMRepo => _gCMRepository = _gCMRepository ?? new GCMRepository(new MySQLDB(_connectionString), _logger);

        public void Dispose()
        {
          
        }
    }
}
