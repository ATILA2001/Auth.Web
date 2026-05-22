using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.DataProtection;
using SharedCookie;

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
            ArgumentNullException.ThrowIfNull(data);

            var dto = SharedCookieTicketDto.FromAspNetCore(data);
            if (dto is null)
            {
                return string.Empty;
            }

            var serialized = Serialize(dto);
            var protectedBytes = _protector.Protect(serialized);
            return Base64UrlEncode(protectedBytes);
        }

        public string Protect(AuthenticationTicket data, string? purpose)
        {
            return Protect(data);
        }

        public AuthenticationTicket? Unprotect(string? protectedText)
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

        public AuthenticationTicket? Unprotect(string? protectedText, string? purpose)
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

        private static SharedCookieTicketDto? Deserialize(byte[] data)
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
}
