﻿using Microsoft.EntityFrameworkCore;
using RKSoftware.DAL.Contract;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace RKSoftware.DAL.EntityFramework
{
    /// <summary>
    /// <see cref="ITransactionalStorage"/> implelmentation using in Entity Framework
    /// </summary>
    public class EntityFrameworkStorage : EntityFrameworkReadonlyStorage, ITransactionalStorage
    {
        private readonly DbContext _dbContext;
        private bool _activeTransaction;
        private static readonly SemaphoreSlim _commitSemaphore = new SemaphoreSlim(1, 1);

        /// <summary>
        /// Initializes a new instance of the <see cref="EntityFrameworkStorage"/> class.
        /// </summary>
        public EntityFrameworkStorage(DbContext context)
            : base(context)
        {
            _dbContext = context ?? throw new ArgumentNullException(nameof(context));
            _activeTransaction = false;
        }

        /// <summary>
        /// <see cref="IStorage.Add{T}(T)"/>
        /// </summary>
        public T Add<T>(T entity) where T : class
        {
            if (entity == null)
            {
                throw new ArgumentNullException(nameof(entity));
            }

            var entry = _dbContext.Set<T>().Add(entity);

            if (!_activeTransaction)
            {
                _commitSemaphore.Wait();
                try
                {
                    if (!_activeTransaction)
                    {
                        _dbContext.SaveChanges();
                    }
                }
                finally
                {
                    _commitSemaphore.Release();
                }
            }

            return entry.Entity;
        }

        /// <summary>
        /// <see cref="IStorage.AddAsync{T}(T)"/>
        /// </summary>
        public async Task<T> AddAsync<T>(T entity) where T : class
        {
            return await AddAsync(entity, CancellationToken.None);
        }

        /// <summary>
        /// <see cref="IStorage.AddAsync{T}(T, CancellationToken)"/>
        /// </summary>
        public async Task<T> AddAsync<T>(T entity, CancellationToken cancellationToken) where T : class
        {
            if (entity == null)
            {
                throw new ArgumentNullException(nameof(entity));
            }

            var entry = _dbContext.Set<T>().Add(entity);

            if (!_activeTransaction)
            {
                await _commitSemaphore.WaitAsync(cancellationToken);
                try
                {
                    if (!_activeTransaction)
                    {
                        if (cancellationToken.IsCancellationRequested)
                        {
                            entry.State = EntityState.Detached;
                            return entity;
                        }

                        await _dbContext.SaveChangesAsync(cancellationToken);
                    }
                }
                finally
                {
                    _commitSemaphore.Release();
                }
            }

            return entry.Entity;
        }

        /// <summary>
        /// <see cref="ITransactionalStorage.BeginTransaction"/>
        /// </summary>
        public void BeginTransaction()
        {
            _activeTransaction = true;
        }

        /// <summary>
        /// <see cref="ITransactionalStorage.CommitTrnsaction"/>
        /// </summary>
        public void CommitTrnsaction()
        {
            _commitSemaphore.Wait();
            try
            {
                _activeTransaction = false;
                _dbContext.SaveChanges();
            }
            finally
            {
                _commitSemaphore.Release();
            }
        }

        /// <summary>
        /// <see cref="IStorage.Remove{T}(T)"/>
        /// </summary>
        public bool Remove<T>(T entity) where T : class
        {
            if (entity == null)
            {
                throw new ArgumentNullException(nameof(entity));
            }

            _dbContext.Remove(entity);
            if (!_activeTransaction)
            {
                _commitSemaphore.Wait();
                try
                {
                    if (!_activeTransaction)
                    {
                        _dbContext.SaveChanges();
                    }
                }
                finally
                {
                    _commitSemaphore.Release();
                }

            }

            return true;
        }

        /// <summary>
        /// <see cref="IStorage.RemoveAsync{T}(T)"/>
        /// </summary>
        public async Task<bool> RemoveAsync<T>(T entity) where T : class
        {
            return await RemoveAsync(entity, CancellationToken.None);
        }

        /// <summary>
        /// <see cref="IStorage.RemoveAsync{T}(T, CancellationToken)"/>
        /// </summary>
        public async Task<bool> RemoveAsync<T>(T entity, CancellationToken cancellationToken) where T : class
        {
            if (entity == null)
            {
                throw new ArgumentNullException(nameof(entity));
            }

            var entry = _dbContext.Remove(entity);
            if (!_activeTransaction)
            {
                await _commitSemaphore.WaitAsync(cancellationToken);
                try
                {
                    if (!_activeTransaction)
                    {
                        if (cancellationToken.IsCancellationRequested)
                        {
                            entry.State = EntityState.Detached;
                            return false;
                        }

                        await _dbContext.SaveChangesAsync(cancellationToken);
                    }
                }
                finally
                {
                    _commitSemaphore.Release();
                }
            }

            return true;
        }

        /// <summary>
        /// <see cref="IStorage.Save{T}(T)"/>
        /// </summary>
        public T Save<T>(T entity) where T : class
        {
            if (entity == null)
            {
                throw new ArgumentNullException(nameof(entity));
            }

            var entry = _dbContext.Set<T>().Attach(entity);

            if (!_activeTransaction)
            {
                _commitSemaphore.Wait();
                try
                {
                    if (!_activeTransaction)
                    {
                        entry.State = EntityState.Modified;
                        _dbContext.SaveChanges();
                    }
                }
                finally
                {
                    _commitSemaphore.Release();
                }
            }

            return entry.Entity;
        }

        /// <summary>
        /// <see cref="IStorage.SaveAsync{T}(T)"/>
        /// </summary>
        public async Task<T> SaveAsync<T>(T entity) where T : class
        {
            return await SaveAsync(entity, CancellationToken.None);
        }

        /// <summary>
        /// <see cref="IStorage.SaveAsync{T}(T, CancellationToken)"/>
        /// </summary>
        public async Task<T> SaveAsync<T>(T entity, CancellationToken cancellationToken) where T : class
        {
            if (entity == null)
            {
                throw new ArgumentNullException(nameof(entity));
            }

            var entry = _dbContext.Set<T>().Attach(entity);

            if (!_activeTransaction)
            {
                await _commitSemaphore.WaitAsync(cancellationToken);
                try
                {
                    if (!_activeTransaction)
                    {
                        if (cancellationToken.IsCancellationRequested)
                        {
                            entry.State = EntityState.Detached;
                            return entity;
                        }

                        entry.State = EntityState.Modified;
                        await _dbContext.SaveChangesAsync(cancellationToken);
                    }
                }
                finally
                {
                    _commitSemaphore.Release();
                }
            }

            return entry.Entity;
        }
    }
}
