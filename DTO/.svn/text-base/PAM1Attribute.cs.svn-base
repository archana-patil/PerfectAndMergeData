using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PerfectAndMergeData.DTO
{
    public class PAM1Attribute
    {
        private string _Id;
        private string _FieldName;
        private string _Value = "";
        private string _Type;
        private string _Entity;
        private string _ActualValue = "";
        private string _SchemaName = "";
        private string _DisplayName = "";
        private string _CustomName = "";
        private string _GroupName = "";
        private string _GroupNo = "";
        private string _AttributeSettingId = "";
        private bool _IsHidden = false;
        private int _DisplayOrder = 0;
        private bool _HandleManually = false;
        private bool _IsPrimary;
        private string _EntityId;
        private int _SectionDisplayOrder = 0;
        private int _MaxLength = 0;

        public int MaxLength { get { return _MaxLength; } set { _MaxLength = value; } }

        public int SectionDisplayOrder
        {
            get { return _SectionDisplayOrder; }
            set { _SectionDisplayOrder = value; }
        }

        public string Id
        {
            get { return _Id; }
            set { _Id = value; }
        }

        public string EntityId
        {
            get { return _EntityId; }
            set { _EntityId = value; }
        }

        public bool IsPrimary
        {
            get { return _IsPrimary; }
            set { _IsPrimary = value; }
        }

        public bool HandleManually
        {
            get { return _HandleManually; }
            set { _HandleManually = value; }
        }


        public int DisplayOrder
        {
            get { return _DisplayOrder; }
            set { _DisplayOrder = value; }
        }

        public bool IsHidden
        {
            get { return _IsHidden; }
            set { _IsHidden = value; }
        }

        public string AttributeSettingId
        {
            get { return _AttributeSettingId; }
            set { _AttributeSettingId = value; }
        }

        public string SchemaName
        {
            get { return _SchemaName; }
            set { _SchemaName = value; }
        }

        public string DisplayName
        {
            get { return _DisplayName; }
            set { _DisplayName = value; }
        }

        public string CustomName
        {
            get { return _CustomName; }
            set { _CustomName = value; }
        }

        public string GroupName
        {
            get { return _GroupName; }
            set { _GroupName = value; }
        }

        public string GroupNo
        {
            get { return _GroupNo; }
            set { _GroupNo = value; }
        }



        public string ActualValue
        {
            get { return _ActualValue; }
            set { _ActualValue = value; }
        }

        public string Entity
        {
            get { return _Entity; }
            set { _Entity = value; }
        }

        public PAM1Attribute(string fieldname)
        {
            this.FieldName = fieldname;
            this.Value = "";
        }
        public PAM1Attribute(string fieldname, string value, string type, string entity)
        {
            this.FieldName = fieldname;
            this.Value = value;
            this.Type = type;
            this.Entity = entity;
        }

        public PAM1Attribute(string fieldname, string entityId, string groupNo, string value, string type, string entity)
        {
            this.FieldName = fieldname;
            this.Value = value;
            this.Type = type;
            this.Entity = entity;
            this.GroupNo = groupNo;
            this.EntityId = entityId;
        }

        public PAM1Attribute(string fieldname, string value, string type, string entity, string ActualValue)
        {
            this.FieldName = fieldname;
            this.Value = value;
            this.Type = type;
            this.Entity = entity;
            this.ActualValue = ActualValue;
        }
        public PAM1Attribute()
        { }

        public string FieldName
        {
            get { return _FieldName; }
            set { _FieldName = value; }
        }

        public string Value
        {
            get
            {
                if (_Value != null)
                    return _Value;
                else
                    return "";
            }
            set { _Value = value; }
        }

        public string Type
        {
            get { return _Type; }
            set { _Type = value; }
        }




    }
}
