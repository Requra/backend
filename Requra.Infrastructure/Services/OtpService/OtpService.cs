using Microsoft.Extensions.Caching.Distributed;
using Requra.Application.Response;
using Requra.Domain.Entities;
using Requra.Domain.Enums;
using Requra.Infrastructure.ExternalInterfaces.IEmailSender;
using Requra.Infrastructure.ExternalServices.EmailSender;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Requra.Infrastructure.Services.OtpService
{
    public class OtpService(IDistributedCache cache, IEmailSender emailSender): Requra.Application.Interfaces.IOtpService.IOtpService
    {
        private static readonly TimeSpan CodeLifetime = TimeSpan.FromMinutes(10);
        private static readonly TimeSpan ResendCooldown = TimeSpan.FromSeconds(60);
        private record OtpEntry(string CodeHash, int Attempts, DateTime CreatedOn);

        private static string CacheKey(string userId, OtpPurpose purpose) => $"otp:{purpose}:{userId}";

        public async Task GenerateAndSendAsync(ApplicationUser user, OtpPurpose purpose)
        {
            var code = GenerateNumericCode();
            var entry = new OtpEntry(Hash(code), 0, DateTime.UtcNow);

            await cache.SetStringAsync(
                CacheKey(user.Id, purpose),
                JsonSerializer.Serialize(entry),
                new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = CodeLifetime });

            await SendEmail(user, code, purpose);
        }

        public async Task<Response<bool>> ResendAsync(ApplicationUser user, OtpPurpose purpose)
        {
            var existing = await GetEntry(user.Id, purpose);
            if (existing != null && DateTime.UtcNow - existing.CreatedOn < ResendCooldown)
            {
                var wait = (int)(ResendCooldown - (DateTime.UtcNow - existing.CreatedOn)).TotalSeconds;
                return Response<bool>.Failure(false, $"Please wait {wait} seconds before requesting a new code.", 429);
            }

            await GenerateAndSendAsync(user, purpose);
            return Response<bool>.Success(true, "A new code has been sent to your email.", 200);
        }

        public async Task<Response<bool>> CheckAsync(ApplicationUser user, string code, OtpPurpose purpose)
        {
            var entry = await GetEntry(user.Id, purpose);

            if (entry == null)
                return Response<bool>.Failure(false, "No active code found. Please request a new one.", 400);

            if (entry.Attempts >= 5)
                return Response<bool>.Failure(false, "Too many failed attempts. Please request a new code.", 400);

            if (entry.CodeHash != Hash(code))
                return Response<bool>.Failure(false, "Invalid code.", 400);

            return Response<bool>.Success(true, "Code is valid.", 200);
        }

        public async Task<Response<bool>> VerifyAsync(ApplicationUser user, string code, OtpPurpose purpose)
        {
            var key = CacheKey(user.Id, purpose);
            var entry = await GetEntry(user.Id, purpose);

            if (entry == null)
                return Response<bool>.Failure(false, "No active code found. Please request a new one.", 400);

            if (entry.Attempts >= 5)
            {
                await cache.RemoveAsync(key);
                return Response<bool>.Failure(false, "Too many failed attempts. Please request a new code.", 400);
            }

            if (entry.CodeHash != Hash(code))
            {
                var remaining = CodeLifetime - (DateTime.UtcNow - entry.CreatedOn);
                await cache.SetStringAsync(key, JsonSerializer.Serialize(entry with { Attempts = entry.Attempts + 1 }),
                    new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = remaining });
                return Response<bool>.Failure(false, "Invalid code.", 400);
            }

            await cache.RemoveAsync(key); // one-time use burned once verified
            return Response<bool>.Success(true, "Code verified.", 200);
        }

        private async Task<OtpEntry?> GetEntry(string userId, OtpPurpose purpose)
        {
            var raw = await cache.GetStringAsync(CacheKey(userId, purpose));
            return raw == null ? null : JsonSerializer.Deserialize<OtpEntry>(raw);
        }

        private static string GenerateNumericCode()
        {
            var bytes = RandomNumberGenerator.GetBytes(4);
            var number = BitConverter.ToUInt32(bytes, 0) % 1000000;
            return number.ToString("D6"); // always 6 digits
        }

        private static string Hash(string code) =>
            Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(code)));

        //private async Task SendEmail(ApplicationUser user, string code, OtpPurpose purpose)
        //{
        //    var subject = purpose == OtpPurpose.EmailConfirmation
        //        ? "Confirm your Requra account" : "Reset your Requra password";

        //    var body = $"""
        //        <p>Hi {user.FullName},</p>
        //        <p>Your verification code is:</p>
        //        <h2>{code}</h2>
        //        <p>This code expires in 10 minutes. If you didn't request this, ignore this email.</p>
        //        """;

        //    await emailSender.SendEmailAsync(user.Email!, subject, body);
        //}
        private async Task SendEmail(ApplicationUser user, string code, OtpPurpose purpose)
        {
            string subject, title, subtitle;

            if (purpose == OtpPurpose.EmailConfirmation)
            {
                subject = "Confirm your Requra account";
                title = "Verify your email";
                subtitle = "enter the code below to confirm your account and get started.";
            }
            else
            {
                subject = "Reset your Requra password";
                title = "Reset your password";
                subtitle = "use the code below to reset your password. If you didn't request this, ignore this email.";
            }

            var body = EmailTemplates.OtpEmail(user.FullName ?? "there", code, title, subtitle);
            await emailSender.SendEmailAsync(user.Email!, subject, body);
        }

    }
}
