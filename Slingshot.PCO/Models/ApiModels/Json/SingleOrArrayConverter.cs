﻿using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace Slingshot.PCO.Models.ApiModels.Json
{
    public class SingleOrArrayConverter<T> : JsonConverter
    {
        public override bool CanConvert( Type objecType )
        {
            return ( objecType == typeof( List<T> ) );
        }

        public override object ReadJson( JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer )
        {
            JToken token = JToken.Load( reader );
            if ( token.Type == JTokenType.Array )
            {
                return token.ToObject<List<T>>();
            }
            return new List<T> { token.ToObject<T>() };
        }

        public override bool CanWrite
        {
            get { return false; }
        }

        public override void WriteJson( JsonWriter writer, object value, JsonSerializer serializer )
        {
            throw new NotImplementedException();
        }
    }
}
