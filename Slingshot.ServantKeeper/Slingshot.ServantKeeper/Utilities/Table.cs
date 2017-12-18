using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Slingshot.ServantKeeper.Utilities
{
    public class Table
    {
        public List<string> ColumnNames { get; set; }
        public List<int> ColumnStart { get; set; }

        public List<List<string>> Data { get; set; }

        public Table()
        {
            ColumnNames = new List<string>();
            ColumnStart = new List<int>();
            Data = new List<List<string>>();
        }

        public List<T> Map<T>() where T : new()
        {
            var mappedObjects = new List<T>();

            foreach ( var row in this.Data )
            {
                mappedObjects.Add( new T() );
            }

            var obj = new T();

            var properties = obj.GetType().GetProperties();
            foreach ( var prop in properties )
            {
                var attributes = prop.CustomAttributes;
                foreach ( var attribute in attributes )
                {
                    if ( attribute.AttributeType.Name == "ColumnName" )
                    {
                        var key = ( ( string ) attribute.ConstructorArguments.FirstOrDefault().Value ).ToLower();
                        for ( var i = 0; i < this.ColumnNames.Count; i++ )
                        {
                            if ( key == this.ColumnNames[i] )
                            {
                                var property = obj.GetType().GetProperty( prop.Name );
                                var propertyType = property.PropertyType;

                                if ( propertyType == typeof( string ) )
                                {
                                    for ( var j = 0; j < mappedObjects.Count; j++ )
                                    {
                                        property.SetValue( mappedObjects[j], this.Data[j][i] );
                                    }
                                }
                                
                                if (propertyType == typeof(bool))
                                {
                                    for (var j = 0; j < mappedObjects.Count; j++)
                                    {
                                        property.SetValue(mappedObjects[j], this.Data[j][i]=="1");
                                    }
                                }

                                if ( propertyType == typeof( int ) )
                                {
                                    for ( var j = 0; j < mappedObjects.Count; j++ )
                                    {
                                        int value = 0;
                                        int.TryParse( this.Data[j][i], out value );
                                        property.SetValue( mappedObjects[j], value );
                                    }
                                }

                                if ( propertyType == typeof( long ) )
                                {
                                    for ( var j = 0; j < mappedObjects.Count; j++ )
                                    {
                                        long value = 0;
                                        long.TryParse( this.Data[j][i], out value );
                                        property.SetValue( mappedObjects[j], value );
                                    }
                                }

                                if ( propertyType == typeof( DateTime ) )
                                {
                                    var parseAttribute = attributes.Where( a => a.AttributeType.Name == "DateTimeParseString" ).FirstOrDefault();
                                    var parseString = "yyyy-MM-dd HH:mm:ss,fff";
                                    if ( parseAttribute != null )
                                    {
                                        parseString = ( string ) parseAttribute.ConstructorArguments.FirstOrDefault().Value;
                                    }

                                    for ( var j = 0; j < mappedObjects.Count; j++ )
                                    {
                                        DateTime value;
                                        if (
                                             DateTime.TryParseExact(
                                                 this.Data[j][i], parseString,
                                                 null,
                                                 System.Globalization.DateTimeStyles.AssumeLocal,
                                                 out value ) )
                                        {
                                            property.SetValue( mappedObjects[j], value );

                                        }
                                    }
                                }

                                if ( propertyType.BaseType == typeof( Enum ) )
                                {
                                    var parseAttribute = attributes.Where( a => a.AttributeType.Name == "MapEnum" ).FirstOrDefault();
                                    var value = 0;
                                    var mapList = new List<string>();
                                    if ( parseAttribute != null )
                                    {
                                        var mapString = ( string ) parseAttribute.ConstructorArguments.FirstOrDefault().Value;
                                        mapList = mapString.Split( new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries ).ToList();
                                    }

                                    for ( var j = 0; j < mappedObjects.Count; j++ )
                                    {
                                        if ( mapList.Contains( this.Data[j][i] ) )
                                        {
                                            property.SetValue( mappedObjects[j], mapList.IndexOf( this.Data[j][i] ) );
                                        }
                                        else
                                        {
                                            property.SetValue( mappedObjects[j], 0 );
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return mappedObjects;
        }
    }
}

