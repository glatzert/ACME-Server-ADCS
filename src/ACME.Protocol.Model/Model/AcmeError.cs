using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using TGIT.ACME.Protocol.Model.Exceptions;
using TGIT.ACME.Protocol.Model.Extensions;

namespace TGIT.ACME.Protocol.Model
{
    [Serializable]
    public class AcmeError : ISerializable
    {
        private string? _type;
        private string? _detail;

        private AcmeError() { }

        public AcmeError(string type, string detail, Identifier? identifier = null, IEnumerable<AcmeError>? subErrors = null)
        {
            Type = type;

            if (!type.Contains(":"))
                Type = "urn:ietf:params:acme:error:" + type;

            Detail = detail;
            Identifier = identifier;
            SubErrors = subErrors?.ToList();
        }

        public string Type { 
            get => _type ?? throw new NotInitializedException();
            private set => _type = value; 
        }
        
        public string Detail { 
            get => _detail ?? throw new NotInitializedException(); 
            set => _detail = value; 
        }

        public Identifier? Identifier { get; }

        public List<AcmeError>? SubErrors { get; }



        // --- Serialization Methods --- //

        protected AcmeError(SerializationInfo info, StreamingContext streamingContext)
        {
            if (info is null)
                throw new ArgumentNullException(nameof(info));

            Type = info.GetRequiredString(nameof(Type));
            Detail = info.GetRequiredString(nameof(Detail));

            Identifier = info.TryGetValue<Identifier>(nameof(Identifier));
            SubErrors = info.TryGetValue<List<AcmeError>>(nameof(SubErrors));
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info is null)
                throw new ArgumentNullException(nameof(info));

            info.AddValue("SerializationVersion", 1);

            info.AddValue(nameof(Type), Type);
            info.AddValue(nameof(Detail), Detail);

            if(Identifier != null) 
                info.AddValue(nameof(Identifier), Identifier);

            if(SubErrors != null)
                info.AddValue(nameof(SubErrors), SubErrors);
        }
    }
}
