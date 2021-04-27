using Core.Repositories;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Core
{
    public interface IUnitOfWork : IDisposable
    {
        IGCMRepository GCMRepo { get; }
        //IMusicRepository Musics { get; }
        // IArtistRepository Artists { get; }
        // Task<int> CommitAsync();
    }
}
