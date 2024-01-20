/* 
{
[2024-01-14T21:44:06.519Z]   "sub": "104594561722614628791",
[2024-01-14T21:44:06.519Z]   "name": "Miłosz D",
[2024-01-14T21:44:06.519Z]   "given_name": "Miłosz",
[2024-01-14T21:44:06.519Z]   "family_name": "D",
[2024-01-14T21:44:06.519Z]   "picture": "https://lh3.googleusercontent.com/a/ACg8ocLysXv4i_yGXM7diLt46NxJZptVhpRTjSZnluBejqVT\u003ds96-c",
[2024-01-14T21:44:06.519Z]   "email": "midub@tlen.pl",
[2024-01-14T21:44:06.519Z]   "email_verified": true,
[2024-01-14T21:44:06.520Z]   "locale": "pl"
[2024-01-14T21:44:06.520Z] }
*/

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
