using DX.Cqrs.Commons;
using DX.Cqrs.Mongo;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using MongoDB.Driver.GridFS;
using System;
using System.IO;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace DX.Cqrs.Services {
    public class MongoFileService : IFileService {
        private readonly IGridFSBucket<IFileID> _bucket;
        private static StreamingContext innerException;
        
        static MongoFileService() {
            RegisterSerializer();
        }

        public MongoFileService(IMongoDatabase database)
            : this(new GridFSBucket<IFileID>(Check.NotNull(database, nameof(database)))) { }

        internal MongoFileService(IGridFSBucket<IFileID> bucket)
            => _bucket = Check.NotNull(bucket, nameof(bucket));

        public async Task<IFileID> Save(Func<Stream, Task> saveAction) {
            MongoFileID id = new MongoFileID();
            string filename = $"{DateTime.UtcNow:yyyy-MM-dd HH-mm-ss.fffffff} {id}";

            using (GridFSUploadStream<IFileID> stream = await _bucket.OpenUploadStreamAsync(id, filename)) {
                await saveAction(stream);
                await stream.CloseAsync();
            }

            return id;
        }

        public async Task Get(IFileID id, Func<Stream, Task> getAction) {
            try {
                using (GridFSDownloadStream<IFileID> stream = await _bucket.OpenDownloadStreamAsync(id)) {
                    await getAction(stream);
                    await stream.CloseAsync();
                }
            } catch (GridFSFileNotFoundException ex) {
                throw CreateFileNotFoundException(id, ex);
            }
        }

        public async Task Delete(IFileID id) {
            try {
                await _bucket.DeleteAsync(id);
            } catch (GridFSFileNotFoundException ex) {
                throw CreateFileNotFoundException(id, ex);
            }
        }
        
        public static void RegisterSerializer() {
            BsonSerializer.RegisterSerializer(MongoFileID.Serializer);
        }

        private static FileNotFoundException CreateFileNotFoundException(
            IFileID id,
            GridFSFileNotFoundException innerException
        ) {
            return new FileNotFoundException($"The file with the ID {id} could not be found.", innerException);
        }

        internal class MongoFileID : Equatable<MongoFileID, ObjectId>, IFileID {
            public static readonly IBsonSerializer<IFileID> Serializer = new CastSerializer<IFileID>(new MongoFileIDSerializer());

            public MongoFileID() : this(ObjectId.GenerateNewId()) { }

            public MongoFileID(ObjectId id) : base(id) { }

            class MongoFileIDSerializer : NullAwareSerializerBase<MongoFileID> {
                protected override MongoFileID DeserializeCore(IBsonReader reader)
                    => new MongoFileID(reader.ReadObjectId());

                protected override void SerializeCore(IBsonWriter writer, MongoFileID value)
                    => writer.WriteObjectId(value.Value);
            }
        }
    }
}
