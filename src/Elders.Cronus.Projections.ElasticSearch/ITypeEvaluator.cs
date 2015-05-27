using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;

namespace Elders.Cronus.Projections.ElasticSearch
{
    public interface ITypeEvaluator
    {
        string Evaluate(object instance);
    }

    public class OverqualifiedNameInspector : ITypeEvaluator
    {
        private int inspectionLimit;

        public OverqualifiedNameInspector(int inspectionLimit)
        {
            this.inspectionLimit = inspectionLimit;
        }

        public string Evaluate(object instance)
        {
            string init = "";
            var queue = new Queue<object>();
            queue.Enqueue(instance);
            while (queue.Count > 0)
            {
                var obj = queue.Dequeue();
                inspectionLimit--;

                if (inspectionLimit == 0)
                {
                    string error = "Inspection limit has been reached. Probably there is a circular reference.";
                    throw new InvalidOperationException(error);
                }

                var t = obj.GetType();
                init += TypeName(t);

                var props = t.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                var withMembers = props
                    .Where(x => x.GetCustomAttribute<DataMemberAttribute>(false) != null)
                    .ToDictionary(x => x.GetCustomAttribute<DataMemberAttribute>().Order)
                    .OrderBy(x => x.Key).ToList();

                foreach (var item in withMembers)
                {
                    var val = item.Value.GetValue(obj);
                    if (val != null)
                        queue.Enqueue(val);
                }

                var fields = t.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                var fieldsWithMembers = fields
                    .Where(x => x.GetCustomAttribute<DataMemberAttribute>(false) != null)
                    .ToDictionary(x => x.GetCustomAttribute<DataMemberAttribute>(false).Order)
                    .OrderBy(x => x.Key)
                    .ToList();

                foreach (var item in fieldsWithMembers)
                {
                    var val = item.Value.GetValue(obj);
                    if (val != null)
                        queue.Enqueue(val);
                }

            }
            var hash = System.Security.Cryptography
                .MD5.Create()
                .ComputeHash(System.Text.Encoding.UTF8.GetBytes(init));

            return Convert.ToBase64String(hash);
        }

        private string TypeName(Type t)
        {
            var attr = t.GetCustomAttribute<DataContractAttribute>(false);
            if (attr != null)
                return attr.Name;
            else
                return t.Name;
        }
    }
}
