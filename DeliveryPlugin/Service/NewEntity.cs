using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
namespace Plugin.Service
{
    public class NewEntity
    {
        public Entity fromEntity { get; set; }
        public Entity toEntity { get; set; }
        public NewEntity(Entity _fromEntity, Entity _toEntity)
        {
            this.fromEntity = _fromEntity;
            this.toEntity = _toEntity;
        }


        public void Set(object value, string toAttribute)
        {
            toEntity[toAttribute] = value;
        }

        public void Set(string fromAttribute, string toAttribute, bool checkNull = false)
        {
            if (checkNull == true)
            {
                if (fromEntity.Contains(fromAttribute) && fromEntity[fromAttribute] != null)
                {
                    toEntity[toAttribute] = fromEntity[fromAttribute];
                }
            }
            else
            {
                if (fromEntity.Contains(fromAttribute))
                {
                    toEntity[toAttribute] = fromEntity[fromAttribute];
                }
            }

        }
        public void Set(string attribute, bool checkNull = false)
        {
            if (checkNull)
            {
                if (fromEntity.Contains(attribute) && fromEntity[attribute] != null)
                {
                    toEntity[attribute] = fromEntity[attribute];
                }
            }
            else
            {
                if (fromEntity.Contains(attribute))
                {
                    toEntity[attribute] = fromEntity[attribute];
                }
            }
        }
    }
}
