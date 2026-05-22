using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;

// This file is the single source of truth for the DataContract serialisation
// format shared between Auth.Web and any client application (e.g. SAI).
// It is linked into client projects via a <Compile> item in their .csproj so
// that both projects always compile against the same definition.
//
// !!  DO NOT change DataMember Order values or the DataContract Namespace  !!
// !!  string — those values are burned into every live cookie in the DB.   !!

namespace SharedCookie
{
    [DataContract(Namespace = "http://schemas.datacontract.org/2004/07/SharedCookie")]
    internal sealed class SharedCookieTicketDto
    {
        [DataMember(Order = 1)]
        public List<ClaimDto> Claims { get; set; } = new();

        [DataMember(Order = 2)]
        public string? AuthenticationType { get; set; }

        [DataMember(Order = 3)]
        public string? NameClaimType { get; set; }

        [DataMember(Order = 4)]
        public string? RoleClaimType { get; set; }

        [DataMember(Order = 5)]
        public Dictionary<string, string?> Properties { get; set; } = new();

        [DataMember(Order = 6)]
        public DateTimeOffset? IssuedUtc { get; set; }

        [DataMember(Order = 7)]
        public DateTimeOffset? ExpiresUtc { get; set; }

        [DataMember(Order = 8)]
        public bool IsPersistent { get; set; }

        [DataMember(Order = 9)]
        public bool? AllowRefresh { get; set; }

        [DataMember(Order = 10)]
        public string? RedirectUri { get; set; }

        public static SharedCookieTicketDto? FromAspNetCore(AuthenticationTicket ticket)
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
                Properties = new Dictionary<string, string?>(ticket.Properties.Items),
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

            var props = new AuthenticationProperties(Properties);
            props.IssuedUtc = IssuedUtc;
            props.ExpiresUtc = ExpiresUtc;
            props.IsPersistent = IsPersistent;
            props.AllowRefresh = AllowRefresh;
            props.RedirectUri = RedirectUri;

            return new AuthenticationTicket(new ClaimsPrincipal(identity), props, AuthenticationType ?? string.Empty);
        }
    }

    [DataContract(Namespace = "http://schemas.datacontract.org/2004/07/SharedCookie")]
    internal sealed class ClaimDto
    {
        [DataMember(Order = 1)]
        public string Type { get; set; } = string.Empty;

        [DataMember(Order = 2)]
        public string Value { get; set; } = string.Empty;

        [DataMember(Order = 3)]
        public string ValueType { get; set; } = ClaimValueTypes.String;

        [DataMember(Order = 4)]
        public string Issuer { get; set; } = ClaimsIdentity.DefaultIssuer;

        [DataMember(Order = 5)]
        public string OriginalIssuer { get; set; } = ClaimsIdentity.DefaultIssuer;

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
