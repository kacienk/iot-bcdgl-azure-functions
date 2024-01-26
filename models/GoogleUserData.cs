using System.Runtime.Serialization;

namespace Iotbcdg.Model
{
    [DataContract]
    public class GoogleUserData
    {
        [DataMember]
        public string sub { set; get; }
        [DataMember]
        public string name { set; get; }
        [DataMember]
        public string given_name { set; get; }
        [DataMember]
        public string family_name { set; get; }
        [DataMember]
        public string picture { set; get; }
        [DataMember]
        public string email { set; get; }
        [DataMember]
        public string email_verified { set; get; }
        [DataMember]
        public string locale { set; get; }

    }
}
