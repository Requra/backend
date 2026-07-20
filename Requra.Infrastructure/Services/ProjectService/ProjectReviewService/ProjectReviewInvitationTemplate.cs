using System;
using System.Net;

namespace Requra.Infrastructure.Services.ProjectService.ProjectReviewService
{
        public static class ProjectReviewInvitationTemplate
        {
            public static string ProjectReviewInvitationEmail(
                string userName,
                string projectName,
                string permission,
                DateTime? expiresAt,
                string reviewUrl,
                string invitedByName,
                string? message = null)
            {
                var safeUserName = WebUtility.HtmlEncode(string.IsNullOrWhiteSpace(userName) ? "User" : userName);
                var safeProjectName = WebUtility.HtmlEncode(string.IsNullOrWhiteSpace(projectName) ? "Project" : projectName);
                var safePermission = WebUtility.HtmlEncode(permission);
                var safeInvitedByName = WebUtility.HtmlEncode(string.IsNullOrWhiteSpace(invitedByName) ? "Requra Team" : invitedByName);
                var safeReviewUrl = WebUtility.HtmlEncode(reviewUrl);
                var safeMessage = WebUtility.HtmlEncode(message ?? string.Empty);

                var expiresText = expiresAt.HasValue
                    ? expiresAt.Value.ToString("dddd, dd MMMM yyyy 'at' hh:mm tt 'UTC'")
                    : "No expiration";

                var messageSection = string.IsNullOrWhiteSpace(safeMessage)
                    ? string.Empty
                    : $"""
                <tr>
                  <td style="padding:0 32px 24px 32px;">
                    <table width="100%" style="background-color:#FCFCFD;border:1px solid #EEF2F7;border-radius:10px;">
                      <tr>
                        <td style="padding:18px 20px;">
                          <p style="margin:0 0 8px 0;font-size:13px;font-weight:700;color:#4B5563;text-transform:uppercase;">
                            Message
                          </p>
                          <p style="margin:0;font-size:14px;line-height:1.8;color:#374151;">
                            {safeMessage}
                          </p>
                        </td>
                      </tr>
                    </table>
                  </td>
                </tr>
                """;

                return $"""
            <!DOCTYPE html>
            <html>
            <body style="margin:0;padding:0;background-color:#f4f5f7;font-family:-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;">
              <table width="100%" style="padding:40px 0;">
                <tr>
                  <td align="center">
                    <table width="560" style="background-color:#ffffff;border-radius:12px;overflow:hidden;box-shadow:0 2px 10px rgba(0,0,0,0.08);">

                      <!-- Header -->
                      <tr>
                        <td style="background-color:#4F46E5;padding:28px 32px;">
                          <span style="color:#ffffff;font-size:20px;font-weight:700;">Requra</span>
                        </td>
                      </tr>

                      <!-- Title -->
                      <tr>
                        <td style="padding:36px 32px 12px 32px;">
                          <h1 style="margin:0 0 10px 0;font-size:26px;color:#111827;">
                            Project Review Invitation
                          </h1>
                          <p style="margin:0;font-size:15px;color:#6B7280;">
                            Hi {safeUserName}, you have been invited to review a project.
                          </p>
                        </td>
                      </tr>

                      <!-- Details Card -->
                      <tr>
                        <td style="padding:0 32px 24px 32px;">
                          <table width="100%" style="background-color:#F9FAFB;border:1px solid #EEF2F7;border-radius:12px;">
                            <tr>
                              <td style="padding:22px;">

                                <p style="margin:0 0 14px 0;font-size:15px;">
                                  <strong>Project:</strong> {safeProjectName}
                                </p>

                                <p style="margin:0 0 14px 0;font-size:15px;">
                                  <strong>Invited By:</strong> {safeInvitedByName}
                                </p>

                                <p style="margin:0 0 14px 0;font-size:15px;">
                                  <strong>Permission:</strong>
                                  <span style="margin-left:6px;padding:5px 12px;border-radius:999px;background:#EEF2FF;color:#4F46E5;font-size:12px;font-weight:700;">
                                    {safePermission}
                                  </span>
                                </p>

                                <p style="margin:0;font-size:15px;">
                                  <strong>Expires:</strong> {expiresText}
                                </p>

                              </td>
                            </tr>
                          </table>
                        </td>
                      </tr>

                      {messageSection}

                      <!-- CTA -->
                      <tr>
                        <td style="padding:0 32px 10px 32px;">
                          <a href="{safeReviewUrl}"
                             style="display:inline-block;background-color:#4F46E5;color:#ffffff;text-decoration:none;padding:13px 24px;border-radius:8px;font-size:14px;font-weight:700;">
                            Review Project
                          </a>
                        </td>
                      </tr>

                      <tr>
                        <td style="padding:0 32px 24px 32px;">
                          <p style="font-size:12px;color:#9CA3AF;">
                            If the button doesn't work, use this link:<br/>
                            <span style="color:#4F46E5;">{safeReviewUrl}</span>
                          </p>
                        </td>
                      </tr>

                      <!-- Footer -->
                      <tr>
                        <td style="padding:20px 32px;background-color:#FAFAFA;border-top:1px solid #F0F0F0;">
                          <p style="margin:0;font-size:12px;color:#B0B0B0;">
                            © {DateTime.UtcNow.Year} Requra. This is an automated email.
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
