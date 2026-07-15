using System;
using System.Collections.Generic;
using System.Text;

namespace Requra.Infrastructure.ExternalServices.EmailSender
{
    public static class EmailTemplates
    {
        public static string OtpEmail(string userName, string code, string title, string subtitle)
        {
            return $$"""
            <!DOCTYPE html>
            <html>
            <body style="margin:0;padding:0;background-color:#f4f5f7;font-family:-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;">
              <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="background-color:#f4f5f7;padding:40px 0;">
                <tr>
                  <td align="center">
                    <table role="presentation" width="480" cellpadding="0" cellspacing="0" style="background-color:#ffffff;border-radius:12px;overflow:hidden;box-shadow:0 1px 4px rgba(0,0,0,0.08);">

                      <!-- Header / logo -->
                      <tr>
                        <td style="background-color:#4F46E5;padding:28px 32px;">
                          <span style="color:#ffffff;font-size:20px;font-weight:700;letter-spacing:0.5px;">Requra</span>
                        </td>
                      </tr>

                      <!-- Body -->
                      <tr>
                        <td style="padding:36px 32px 8px 32px;">
                          <h1 style="margin:0 0 8px 0;font-size:20px;color:#111827;">{{title}}</h1>
                          <p style="margin:0 0 24px 0;font-size:14px;line-height:1.6;color:#6B7280;">
                            Hi {{userName}}, {{subtitle}}
                          </p>
                        </td>
                      </tr>

                      <!-- OTP code block -->
                      <tr>
                        <td style="padding:0 32px 24px 32px;">
                          <table role="presentation" width="100%" cellpadding="0" cellspacing="0">
                            <tr>
                              <td align="center" style="background-color:#F5F5FF;border-radius:8px;padding:20px;">
                                <span style="font-size:32px;font-weight:700;letter-spacing:8px;color:#4F46E5;">{{code}}</span>
                              </td>
                            </tr>
                          </table>
                        </td>
                      </tr>

                      <!-- Expiry note -->
                      <tr>
                        <td style="padding:0 32px 32px 32px;">
                          <p style="margin:0;font-size:13px;line-height:1.6;color:#9CA3AF;">
                            This code expires in <strong>10 minutes</strong>. If you didn't request this, you can safely ignore this email — your account is still secure.
                          </p>
                        </td>
                      </tr>

                      <!-- Footer -->
                      <tr>
                        <td style="padding:20px 32px;background-color:#FAFAFA;border-top:1px solid #F0F0F0;">
                          <p style="margin:0;font-size:12px;color:#B0B0B0;">
                            © {{DateTime.UtcNow.Year}} Requra. This is an automated message, please don't reply.
                          </p>
                        </td>
                      </tr>

                    </table>
                  </td>
                </tr>
              </table>
            </body>
            </html>
            """;
        }
    }
}
