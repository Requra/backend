using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace Requra.Infrastructure.Services.InvitationService.MeetingInvitationService
{
    public static class MeetingInvitationTemplete
    {
            public static string MeetingInvitationEmail(string userName,string meetingTitle,string inviteType,string meetingRole,DateTime? scheduledAt,DateTime? expiresAt,string? meetingUrl,string invitedByName,string? meetingDescription = null,bool isGuest = false)
            {
                var safeUserName = WebUtility.HtmlEncode(string.IsNullOrWhiteSpace(userName) ? "User" : userName);
                var safeMeetingTitle = WebUtility.HtmlEncode(string.IsNullOrWhiteSpace(meetingTitle) ? "Meeting" : meetingTitle);
                var safeInviteType = WebUtility.HtmlEncode(inviteType);
                var safeMeetingRole = WebUtility.HtmlEncode(meetingRole);
                var safeInvitedByName = WebUtility.HtmlEncode(string.IsNullOrWhiteSpace(invitedByName) ? "Requra Team" : invitedByName);
                var safeMeetingDescription = WebUtility.HtmlEncode(meetingDescription ?? string.Empty);
                var safeMeetingUrl = string.IsNullOrWhiteSpace(meetingUrl)
                    ? null
                    : WebUtility.HtmlEncode(meetingUrl);

                var introText = isGuest
                    ? "You have been invited as a guest to join a meeting on Requra."
                    : "You have been invited to join a meeting on Requra.";

                var scheduledText = scheduledAt.HasValue
                    ? scheduledAt.Value.ToString("dddd, dd MMMM yyyy 'at' hh:mm tt 'UTC'")
                    : "To be announced";

                var expiresText = expiresAt.HasValue
                    ? expiresAt.Value.ToString("dddd, dd MMMM yyyy 'at' hh:mm tt 'UTC'")
                    : "N/A";

                var descriptionSection = string.IsNullOrWhiteSpace(safeMeetingDescription)
                    ? string.Empty
                    : $"""
                <tr>
                  <td style="padding:0 32px 24px 32px;">
                    <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="background-color:#FCFCFD;border:1px solid #EEF2F7;border-radius:10px;">
                      <tr>
                        <td style="padding:18px 20px;">
                          <p style="margin:0 0 8px 0;font-size:13px;font-weight:700;color:#4B5563;text-transform:uppercase;letter-spacing:0.4px;">
                            Meeting Description
                          </p>
                          <p style="margin:0;font-size:14px;line-height:1.8;color:#374151;">
                            {safeMeetingDescription}
                          </p>
                        </td>
                      </tr>
                    </table>
                  </td>
                </tr>
                """;

                var meetingUrlSection = string.IsNullOrWhiteSpace(safeMeetingUrl)
                    ? """
                <tr>
                  <td style="padding:0 32px 24px 32px;">
                    <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="background-color:#FFF7ED;border:1px solid #FED7AA;border-radius:10px;">
                      <tr>
                        <td style="padding:16px 18px;">
                          <p style="margin:0;font-size:14px;line-height:1.7;color:#9A3412;">
                            The meeting link is not available yet. It will be shared with you once it is ready.
                          </p>
                        </td>
                      </tr>
                    </table>
                  </td>
                </tr>
                """
                    : $"""
                <tr>
                  <td style="padding:0 32px 10px 32px;">
                    <a href="{safeMeetingUrl}"
                       style="display:inline-block;background-color:#4F46E5;color:#ffffff;text-decoration:none;padding:13px 24px;border-radius:8px;font-size:14px;font-weight:700;">
                      Join Meeting
                    </a>
                  </td>
                </tr>
                <tr>
                  <td style="padding:0 32px 24px 32px;">
                    <p style="margin:0;font-size:12px;line-height:1.7;color:#9CA3AF;word-break:break-all;">
                      If the button doesn't work, copy and paste this link into your browser:<br/>
                      <span style="color:#4F46E5;">{safeMeetingUrl}</span>
                    </p>
                  </td>
                </tr>
                """;

                return $"""
            <!DOCTYPE html>
            <html>
            <body style="margin:0;padding:0;background-color:#f4f5f7;font-family:-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;">
              <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="background-color:#f4f5f7;padding:40px 0;">
                <tr>
                  <td align="center">
                    <table role="presentation" width="560" cellpadding="0" cellspacing="0" style="background-color:#ffffff;border-radius:12px;overflow:hidden;box-shadow:0 2px 10px rgba(0,0,0,0.08);">

                      <tr>
                        <td style="background-color:#4F46E5;padding:28px 32px;">
                          <span style="color:#ffffff;font-size:20px;font-weight:700;letter-spacing:0.5px;">Requra</span>
                        </td>
                      </tr>

                      <tr>
                        <td style="padding:36px 32px 12px 32px;">
                          <h1 style="margin:0 0 10px 0;font-size:28px;line-height:1.3;color:#111827;">Meeting Invitation</h1>
                          <p style="margin:0;font-size:15px;line-height:1.8;color:#6B7280;">
                            Hi {safeUserName}, {introText}
                          </p>
                        </td>
                      </tr>

                      <tr>
                        <td style="padding:0 32px 24px 32px;">
                          <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="background-color:#F9FAFB;border:1px solid #EEF2F7;border-radius:12px;">
                            <tr>
                              <td style="padding:22px;">
                                <p style="margin:0 0 14px 0;font-size:16px;color:#111827;">
                                  <strong>Meeting:</strong> {safeMeetingTitle}
                                </p>

                                <p style="margin:0 0 14px 0;font-size:15px;color:#111827;">
                                  <strong>Scheduled For:</strong> {scheduledText}
                                </p>

                                <p style="margin:0 0 14px 0;font-size:15px;color:#111827;">
                                  <strong>Invited By:</strong> {safeInvitedByName}
                                </p>

                                <p style="margin:0 0 14px 0;font-size:15px;color:#111827;">
                                  <strong>Invite Type:</strong>
                                  <span style="display:inline-block;margin-left:6px;padding:5px 12px;border-radius:999px;background-color:#ECFDF5;color:#059669;font-size:12px;font-weight:700;">
                                    {safeInviteType}
                                  </span>
                                </p>

                                <p style="margin:0 0 14px 0;font-size:15px;color:#111827;">
                                  <strong>Your Role:</strong>
                                  <span style="display:inline-block;margin-left:6px;padding:5px 12px;border-radius:999px;background-color:#EEF2FF;color:#4F46E5;font-size:12px;font-weight:700;">
                                    {safeMeetingRole}
                                  </span>
                                </p>

                                <p style="margin:0;font-size:15px;color:#111827;">
                                  <strong>Invitation Expires:</strong> {expiresText}
                                </p>
                              </td>
                            </tr>
                          </table>
                        </td>
                      </tr>

                      {descriptionSection}

                      <tr>
                        <td style="padding:0 32px 8px 32px;">
                          <p style="margin:0;font-size:14px;line-height:1.8;color:#374151;">
                            Please review the invitation details above and use the meeting link below to join at the scheduled time.
                          </p>
                        </td>
                      </tr>

                      {meetingUrlSection}

                      <tr>
                        <td style="padding:0 32px 32px 32px;">
                          <p style="margin:0;font-size:13px;line-height:1.8;color:#9CA3AF;">
                            If you were not expecting this invitation, you can safely ignore this email.
                          </p>
                        </td>
                      </tr>

                      <tr>
                        <td style="padding:20px 32px;background-color:#FAFAFA;border-top:1px solid #F0F0F0;">
                          <p style="margin:0;font-size:12px;color:#B0B0B0;">
                            © {DateTime.UtcNow.Year} Requra. This is an automated message, please don't reply.
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
