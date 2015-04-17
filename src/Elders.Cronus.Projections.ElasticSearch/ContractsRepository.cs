using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Elders.Cronus.DomainModeling;

namespace Elders.Cronus.Projections.ElasticSearch
{
    public interface IContractsRepository
    {
        IEnumerable<Type> Contracts { get; }

        bool TryGet(Type type, out string name);
        bool TryGet(string name, out Type type);
    }

    public class ContractsRepository : IContractsRepository
    {
        readonly Dictionary<Type, string> typeToName = new Dictionary<Type, string>();
        readonly Dictionary<string, Type> nameToType = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);

        public ContractsRepository(IEnumerable<Type> contracts)
        {
            if (contracts != null)
            {
                foreach (var contract in contracts)
                {
                    if (contract.HasAttribute<DataContractAttribute>())
                        Map(contract, contract.GetAttrubuteValue<DataContractAttribute, string>(x => x.Name));
                }
            }
        }

        public bool TryGet(Type type, out string name)
        {
            return typeToName.TryGetValue(type, out name);
        }

        public bool TryGet(string name, out Type type)
        {
            return nameToType.TryGetValue(name, out type);
        }

        public IEnumerable<Type> Contracts { get { return typeToName.Keys.ToList().AsReadOnly(); } }

        private void Map(Type type, string name)
        {
            typeToName.Add(type, name);
            nameToType.Add(name, type);
        }
    }
}
