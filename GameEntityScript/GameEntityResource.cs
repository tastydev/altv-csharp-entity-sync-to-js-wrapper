using System;
using System.Collections.Generic;
using System.Numerics;
using AltV.Net;
using AltV.Net.EntitySync;
using AltV.Net.EntitySync.ServerEvent;
using AltV.Net.EntitySync.SpatialPartitions;

namespace GameEntityScript
{
    public enum EntityType : ulong
    {
        Object = 0,
        Ped = 1,
        TextLabel = 2,
        Marker = 3,
    }
    public class GameEntityResource : Resource 
	{
        
        private void InitEntitySync()
        {
            AltEntitySync.Init(
                4,
                (threadId) => 100,
                (threadId) => false, //netOwner calculations disabled (no need currently)
                (threadCount, repository) => new ServerEventNetworkLayer(threadCount, repository),
                (entity, threadCount) => entity.Type,
                (entityId, entityType, threadCount) => entityType,
                (threadId) =>
                {
                    if (threadId == (ulong)EntityType.Object) // objects
                        return new LimitedGrid3(50_000, 50_000, 100, 10_000, 10_000, 350);
                    if (threadId == (ulong)EntityType.Ped) // peds
                        return new LimitedGrid3(50_000, 50_000, 100, 10_000, 10_000, 125);
                    if (threadId == (ulong)EntityType.TextLabel) // text labels
                        return new LimitedGrid3(50_000, 50_000, 100, 10_000, 10_000, 125);
                    if (threadId == (ulong)EntityType.Marker) // marker
                        return new LimitedGrid3(50_000, 50_000, 100, 10_000, 10_000, 125);
                    return null;
                },
                new IdProvider()
            );
        }

        private void RegisterExports()
        {
            Alt.Export("createGameEntity", new Func<long, Vector3, int, int, IDictionary<string, object>, ulong>(this.CreateGameEntity));
            Alt.Export("removeGameEntity", new Action<long, long>(this.RemoveGameEntity));
            Alt.Export("removeAllGameEntities", new Action(this.RemoveAllGameEntities));
            Alt.Export("doesGameEntityExist", new Func<long, long, bool>(this.DoesGameEntityExist));
            Alt.Export("setGameEntityPosition", new Action<long, long, Vector3>(this.SetGameEntityPosition));
            Alt.Export("getGameEntityPosition", new Func<long, long, Vector3>(this.GetGameEntityPosition));
            Alt.Export("getGameEntityRange", new Func<long, long, uint>(this.GetGameEntityRange));
            Alt.Export("setGameEntityDimension", new Action<long, long, int>(this.SetGameEntityDimension));
            Alt.Export("getGameEntityDimension", new Func<long, long, int>(this.GetGameEntityDimension));
            Alt.Export("setGameEntityData", new Action<long, long, String, object>(this.SetGameEntityData));
            Alt.Export("getGameEntityData", new Func<long, long, String, object>(this.GetGameEntityData));
            Alt.Export("resetGameEntityData", new Action<long, long, String>(this.ResetGameEntityData));
        }

        private IEntity GetGameEntity(long id, long type)
        {
            IEntity entity;

            if (!AltEntitySync.TryGetEntity((ulong)id, (ulong)type, out entity))
                return null;

            return entity;
        }
       
        private ulong CreateGameEntity(long type, Vector3 position, int dimension, int range, IDictionary<string, object> data)
        {
            IEntity entity = AltEntitySync.CreateEntity((ulong) type, position, dimension, (uint) range, data);
            return entity.Id;
        }

        private void RemoveGameEntity(long id, long type)
        {
            IEntity entity = this.GetGameEntity(id, type);

            AltEntitySync.RemoveEntity(entity);
        }

        private void RemoveAllGameEntities()
        {
            IEnumerable<IEntity> entities = AltEntitySync.GetAllEntities();

            foreach (IEntity entity in entities)
            {
               AltEntitySync.RemoveEntity(entity);
            }
              
        }

        private bool DoesGameEntityExist(long id, long type)
        {
            IEntity entity = GetGameEntity(id, type);

            return entity != null;
        }

        private void SetGameEntityPosition(long id, long type, Vector3 position)
        {
            IEntity entity = GetGameEntity(id, type);

            if (entity == null)
            {
                Console.WriteLine("[WARN] EntitySyncWrapper SetGameEntityPosition was called with invalid entity!");
                return;
            }

            entity.Position = position;
        }

        private Vector3 GetGameEntityPosition(long id, long type)
        {
            IEntity entity = GetGameEntity(id, type);

            if (entity == null)
            {
                Console.WriteLine("[WARN] EntitySyncWrapper GetGameEntityPosition was called with invalid entity!");
                return new Vector3();
            }

            return entity.Position;
        }

        private uint GetGameEntityRange(long id, long type)
        {
            IEntity entity = GetGameEntity(id, type);

            if (entity == null)
            {
                Console.WriteLine("[WARN] EntitySyncWrapper GetGameEntityRange was called with invalid entity!");
                return 0;
            }

            return entity.Range;
        }

        private void SetGameEntityDimension(long id, long type, int dimension)
        {
            IEntity entity = GetGameEntity(id, type);

            if (entity == null)
            {
                Console.WriteLine("[WARN] EntitySyncWrapper SetGameEntityDimension was called with invalid entity!");
                return;
            }

            entity.Dimension = dimension;
        }

        private int GetGameEntityDimension(long id, long type)
        {
            IEntity entity = GetGameEntity(id, type);

            if (entity == null)
            {
                Console.WriteLine("[WARN] EntitySyncWrapper GetGameEntityDimension was called with invalid entity!");
                return 0;
            }

            return entity.Dimension;
        }

        private void SetGameEntityData(long id, long type, String key, object value)
        {
            IEntity entity = GetGameEntity(id, type);

            if(entity == null)
            {
                Console.WriteLine("[WARN] EntitySyncWrapper SetGameEntityData was called with invalid entity!");
                return;
            }

            if (value == null)
                entity.ResetData(key);

            else 
                entity.SetData(key, value);
        }

        private object GetGameEntityData(long id, long type, String key)
        {
            IEntity entity = GetGameEntity(id, type);

            if (entity == null)
            {
                Console.WriteLine("[WARN] EntitySyncWrapper GetGameEntityData was called with invalid entity!");
                return null;
            }

            object result;

            if(!entity.TryGetData(key, out result))
            {
                Console.WriteLine("[WARN] EntitySyncWrapper GetGameEntityData was called with invalid data key!");
                return null;
            }

            return result;
        }

        private void ResetGameEntityData(long id, long type, String key)
        {
            this.SetGameEntityData(id, type, key, null);
        }

        public override void OnStart()
        {
            this.InitEntitySync();
            this.RegisterExports();
            Console.WriteLine("[INFO] EntitySyncWrapper registered!");
        }

        public override void OnStop()
        {
            this.RemoveAllGameEntities();
            AltEntitySync.Stop();
            Console.WriteLine("[INFO] EntitySyncWrapper stopped!");
        }
    }
}
