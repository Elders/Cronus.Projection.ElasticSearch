using System;
using System.Reflection;
using System.Runtime.Serialization;
using Elders.Cronus.DomainModeling;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Elders.Cronus.Projections.ElasticSearch
{
    internal class Json
    {
        JsonSerializerSettings settings;

        public Json(IContractsRepository contractRepository)
        {
            settings = new JsonSerializerSettings();
            settings.ContractResolver = new CamelCasePropertyNamesContractResolver();
            settings.DateTimeZoneHandling = DateTimeZoneHandling.Utc;
            settings.DateFormatHandling = DateFormatHandling.IsoDateFormat;
            settings.ContractResolver = new DataMemberContractResolver();
            settings.TypeNameHandling = TypeNameHandling.Objects;
            settings.TypeNameAssemblyFormat = System.Runtime.Serialization.Formatters.FormatterAssemblyStyle.Simple;
            settings.Formatting = Formatting.Indented;
            settings.Binder = new TypeNameSerializationBinder(contractRepository);
            settings.ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor;
        }

        public string Serialize(object obj)
        {
            return JsonConvert.SerializeObject(obj, settings);
        }

        public object Deserialize(string str)
        {
            return JsonConvert.DeserializeObject(str, settings);
        }

        public T Deserialize<T>(string str)
        {
            return JsonConvert.DeserializeObject<T>(str, settings);
        }

        class DataMemberContractResolver : DefaultContractResolver
        {
            protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
            {
                JsonProperty customMember = base.CreateProperty(member, memberSerialization);
                if (member.HasAttribute<DataMemberAttribute>())
                    customMember.PropertyName = customMember.Order.ToString();

                return customMember;
            }
        }

        class TypeNameSerializationBinder : SerializationBinder
        {
            static log4net.ILog log = log4net.LogManager.GetLogger(typeof(TypeNameSerializationBinder));

            private readonly IContractsRepository contractRepository;

            public TypeNameSerializationBinder(IContractsRepository contractRepository)
            {
                this.contractRepository = contractRepository;
            }

            public override void BindToName(Type serializedType, out string assemblyName, out string typeName)
            {
                string name;
                if (contractRepository.TryGet(serializedType, out name))
                {
                    assemblyName = null;
                    typeName = name;
                }
                else
                {
                    assemblyName = serializedType.Assembly.FullName;
                    typeName = serializedType.FullName;
                }
            }

            public override Type BindToType(string assemblyName, string typeName)
            {
                try
                {
                    if (assemblyName == null)
                    {
                        Type type;
                        if (contractRepository.TryGet(typeName, out type))
                            return type;
                    }
                    return Type.GetType(string.Format("{0}, {1}", typeName, assemblyName), true);
                }
                catch (TypeLoadException ex)
                {
                    string error = String.Format("Cannot resolve type '{0}'. Probably the type was renamed or an object was serialized without DataContractAttribute on first place. In order to not break the rest of the results this record will not be deserialized and default value will be returned. You should manually fix this within the search index.", typeName);
                    log.Error(error, ex);
                    return typeof(string);
                }
            }
        }
    }
}
