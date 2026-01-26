using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.DataProtection;

namespace Auth.Web.Security
{
    public sealed class SharedCookieTicketDataFormat : ISecureDataFormat<AuthenticationTicket>
    {
        private readonly IDataProtector _protector;
        private static readonly DataContractSerializer Serializer = new DataContractSerializer(typeof(SharedCookieTicketDto));

        public SharedCookieTicketDataFormat(IDataProtector protector)
        {
            _protector = protector ?? throw new ArgumentNullException(nameof(protector));
        }

        public string Protect(AuthenticationTicket data)
        {
            if (data == null)
            {
                return null;
            }

            var dto = SharedCookieTicketDto.FromAspNetCore(data);
            var serialized = Serialize(dto);
            var protectedBytes = _protector.Protect(serialized);
            return Base64UrlEncode(protectedBytes);
        }

        public string Protect(AuthenticationTicket data, string purpose)
        {
            return Protect(data);
        }

        public AuthenticationTicket Unprotect(string protectedText)
        {
            if (string.IsNullOrWhiteSpace(protectedText))
            {
                return null;
            }

            try
            {
                var protectedBytes = Base64UrlDecode(protectedText);
                var unprotectedBytes = _protector.Unprotect(protectedBytes);
                var dto = Deserialize(unprotectedBytes);
                return dto != null ? dto.ToAspNetCore() : null;
            }
            catch
            {
                return null;
            }
        }

        public AuthenticationTicket Unprotect(string protectedText, string purpose)
        {
            return Unprotect(protectedText);
        }

        private static byte[] Serialize(SharedCookieTicketDto dto)
        {
            using (var ms = new MemoryStream())
            {
                Serializer.WriteObject(ms, dto);
                return ms.ToArray();
            }
        }

        private static SharedCookieTicketDto Deserialize(byte[] data)
        {
            using (var ms = new MemoryStream(data))
            {
                return Serializer.ReadObject(ms) as SharedCookieTicketDto;
            }
        }

        private static string Base64UrlEncode(byte[] input)
        {
            var base64 = Convert.ToBase64String(input);
            return base64.TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');
        }

        private static byte[] Base64UrlDecode(string input)
        {
            var s = input.Replace('-', '+').Replace('_', '/');
            switch (s.Length % 4)
            {
                case 2: s += "=="; break;
                case 3: s += "="; break;
            }
            return Convert.FromBase64String(s);
        }
    }

    [DataContract(Namespace = "http://schemas.datacontract.org/2004/07/SharedCookie")]
    internal sealed class SharedCookieTicketDto
    {
        [DataMember(Order = 1)]
        public List<ClaimDto> Claims { get; set; }

        [DataMember(Order = 2)]
        public string AuthenticationType { get; set; }

        [DataMember(Order = 3)]
        public string NameClaimType { get; set; }

        [DataMember(Order = 4)]
        public string RoleClaimType { get; set; }

        [DataMember(Order = 5)]
        public Dictionary<string, string> Properties { get; set; }

        [DataMember(Order = 6)]
        public DateTimeOffset? IssuedUtc { get; set; }

        [DataMember(Order = 7)]
        public DateTimeOffset? ExpiresUtc { get; set; }

        [DataMember(Order = 8)]
        public bool IsPersistent { get; set; }

        [DataMember(Order = 9)]
        public bool? AllowRefresh { get; set; }

        [DataMember(Order = 10)]
        public string RedirectUri { get; set; }

        public static SharedCookieTicketDto FromAspNetCore(AuthenticationTicket ticket)
        {
            var identity = ticket.Principal.Identity as ClaimsIdentity;
            if (identity == null)
            {
                return null;
            }

            return new SharedCookieTicketDto
            {
                AuthenticationType = identity.AuthenticationType,
                NameClaimType = identity.NameClaimType,
                RoleClaimType = identity.RoleClaimType,
                Claims = identity.Claims.Select(ClaimDto.FromClaim).ToList(),
                Properties = new Dictionary<string, string>(ticket.Properties.Items ?? new Dictionary<string, string>()),
                IssuedUtc = ticket.Properties.IssuedUtc,
                ExpiresUtc = ticket.Properties.ExpiresUtc,
                IsPersistent = ticket.Properties.IsPersistent,
                AllowRefresh = ticket.Properties.AllowRefresh,
                RedirectUri = ticket.Properties.RedirectUri
            };
        }

        public AuthenticationTicket ToAspNetCore()
        {
            var identity = new ClaimsIdentity(
                Claims != null ? Claims.Select(c => c.ToClaim()) : Enumerable.Empty<Claim>(),
                AuthenticationType,
                NameClaimType ?? ClaimTypes.Name,
                RoleClaimType ?? ClaimTypes.Role);

            var props = new AuthenticationProperties(Properties ?? new Dictionary<string, string>());
            props.IssuedUtc = IssuedUtc;
            props.ExpiresUtc = ExpiresUtc;
            props.IsPersistent = IsPersistent;
            props.AllowRefresh = AllowRefresh;
            props.RedirectUri = RedirectUri;

            return new AuthenticationTicket(new ClaimsPrincipal(identity), props, AuthenticationType);
        }
    }

    [DataContract(Namespace = "http://schemas.datacontract.org/2004/07/SharedCookie")]
    internal sealed class ClaimDto
    {
        [DataMember(Order = 1)]
        public string Type { get; set; }

        [DataMember(Order = 2)]
        public string Value { get; set; }

        [DataMember(Order = 3)]
        public string ValueType { get; set; }

        [DataMember(Order = 4)]
        public string Issuer { get; set; }

        [DataMember(Order = 5)]
        public string OriginalIssuer { get; set; }

        public static ClaimDto FromClaim(Claim claim)
        {
            return new ClaimDto
            {
                Type = claim.Type,
                Value = claim.Value,
                ValueType = claim.ValueType,
                Issuer = claim.Issuer,
                OriginalIssuer = claim.OriginalIssuer
            };
        }

        public Claim ToClaim()
        {
            return new Claim(Type, Value, ValueType ?? ClaimValueTypes.String, Issuer ?? ClaimsIdentity.DefaultIssuer, OriginalIssuer ?? ClaimsIdentity.DefaultIssuer);
        }
    }
}
