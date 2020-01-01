using DX.Contracts.Serialization;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using System;
using JsonToken = Newtonsoft.Json.JsonToken;

namespace DX.Cqrs.Mongo.Serialization {
    public class MongoReaderAdapter : OptimizedJsonReader {
        private readonly IBsonReader _bson;

        public MongoReaderAdapter(IBsonReader bsonReader)
            => _bson = Check.NotNull(bsonReader, nameof(bsonReader));

        public override bool ReadDiscriminatorOrNull(string propertyName, out string? value) {
            BsonReaderBookmark state = _bson.GetBookmark();

            if (_bson.State == BsonReaderState.Value) {
                _bson.ReadNull();
                value = null;
                return false;
            }

            value = _bson.FindStringElement(propertyName);
            _bson.ReturnToBookmark(state);
            return true;
        }

        public override bool Read() {
            if (_bson.State == BsonReaderState.Type)
                _bson.ReadBsonType();

            switch (_bson.State) {
                case BsonReaderState.Initial:
                    _bson.ReadStartDocument();
                    SetToken(JsonToken.StartObject);
                    break;

                case BsonReaderState.EndOfDocument:
                    _bson.ReadEndDocument();
                    SetToken(JsonToken.EndObject);
                    break;

                case BsonReaderState.EndOfArray:
                    _bson.ReadEndArray();
                    SetToken(JsonToken.EndArray);
                    break;

                case BsonReaderState.Name:
                    SetToken(JsonToken.PropertyName, _bson.ReadName());
                    break;

                case BsonReaderState.Value:
                    switch (_bson.CurrentBsonType) {
                        case BsonType.Document:
                            _bson.ReadStartDocument();
                            SetToken(JsonToken.StartObject);
                            break;
                        case BsonType.Array:
                            _bson.ReadStartArray();
                            SetToken(JsonToken.StartArray);
                            break;
                        default:
                            ReadValue(_bson.CurrentBsonType);
                            break;
                    }
                    break;

                case BsonReaderState.Done:
                case BsonReaderState.Closed:
                    return false;

                case BsonReaderState.ScopeDocument:
                default:
                    throw new NotImplementedException();
            }

            return true;
        }

        private void ReadValue(BsonType type) {
            if (type == BsonType.Null) {
                _bson.ReadNull();
                SetToken(JsonToken.Null, null);
            } else if (type == BsonType.Binary) {
                ReadBinaryData();
            } else {
                (JsonToken token, object? value) = type switch
                {
                    BsonType.String => (JsonToken.String, (object)_bson.ReadString()),
                    BsonType.Int32 => (JsonToken.Integer, _bson.ReadInt32()),
                    BsonType.Int64 => (JsonToken.Integer, _bson.ReadInt64()),
                    BsonType.Double => (JsonToken.Float, _bson.ReadDouble()),
                    BsonType.Decimal128 => (JsonToken.Float, (decimal)_bson.ReadDecimal128()),
                    BsonType.Boolean => (JsonToken.Boolean, _bson.ReadBoolean()),
                    BsonType.DateTime => (JsonToken.Date, ToDateTime(_bson.ReadDateTime())),

                    // These are not expected (handled otherwise):
                    //   BsonType.EndOfDocument
                    //   BsonType.Document
                    //   BsonType.Array 
                    // These make no sense or are not supported:
                    // (see also https://docs.mongodb.com/manual/reference/bson-types/)
                    //   BsonType.Undefined
                    //   BsonType.RegularExpression
                    //   BsonType.JavaScript
                    //   BsonType.Symbol
                    //   BsonType.JavaScriptWithScope
                    //   BsonType.MinKey
                    //   BsonType.MaxKey
                    //   BsonType.Timestamp
                    //   BsonType.ObjectId
                    _ => throw new NotImplementedException()
                };

                SetToken(token, value);
            }
        }

        private void ReadBinaryData() {
            BsonBinaryData data = _bson.ReadBinaryData();

            object value = data.SubType switch
            {
                BsonBinarySubType.Binary => (object)data.Bytes,
                BsonBinarySubType.UuidStandard => data.AsGuid,
                _ => throw new NotImplementedException()
            };

            SetToken(JsonToken.Bytes, value);
        }

        private static DateTime ToDateTime(long millisecondsSinceEpoch) {
            DateTime utc = BsonUtils.ToDateTimeFromMillisecondsSinceEpoch(millisecondsSinceEpoch);
            return utc.ToLocalTime();
        }
    }
}

