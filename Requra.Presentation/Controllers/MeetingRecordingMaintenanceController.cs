using Google;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Requra.Infrastructure.Data;

namespace Requra.Presentation.Controllers
{
    [Route("api/test/meeting-recordings")]
    public class MeetingRecordingMaintenanceController : ControllerBase
    {
        private readonly RequraDbContext _context;

        public MeetingRecordingMaintenanceController(RequraDbContext context)
        {
            _context = context;
        }

        [HttpPost("sync-all-recording-urls")]
        public async Task<IActionResult> SyncAllRecordingUrlsToMeetings(CancellationToken cancellationToken)
        {
            try
            {
                var meetings = await _context.MeetingSessions.Include(x => x.Recordings).ToListAsync(cancellationToken);
                    

                if (!meetings.Any())
                {
                    return NotFound(new
                    {
                        message = "No meetings found."
                    });
                }

                var result = new List<object>();
                var meetingsWithRecordings = 0;
                var meetingsWithoutRecordings = 0;
                var totalUrlsMapped = 0;

                foreach (var meeting in meetings)
                {
                    var urls = meeting.Recordings
                        .Where(r => !string.IsNullOrWhiteSpace(r.StorageUrl)&&r.ReceivedBytes > 0&&r.UploadedChunks>0)
                        .OrderByDescending(r => r.CompletedAt ??  r.CreatedAt)
                        .Select(r => r.StorageUrl!)
                        .Distinct()
                        .ToList();

                    if (urls.Any())
                    {
                        meetingsWithRecordings++;
                        totalUrlsMapped += urls.Count;
                        meeting.SetRecordingUrls(urls);
                        await _context.SaveChangesAsync(cancellationToken);

                    }
                    else
                    {
                        meetingsWithoutRecordings++;
                        meeting.SetRecordingUrls(new List<string>());
                        await _context.SaveChangesAsync(cancellationToken);
                    }

                    result.Add(new
                    {
                        meetingId = meeting.Id,
                        title = meeting.Title,
                        hasRecordings = urls.Any(),
                        recordingCount = urls.Count,
                        recordingUrls = urls
                    });
                }

                await _context.SaveChangesAsync(cancellationToken);

                return Ok(new
                {
                    message = "All meetings were checked and recording URLs were mapped successfully.",
                    totalMeetings = meetings.Count,
                    meetingsWithRecordings,
                    meetingsWithoutRecordings,
                    totalUrlsMapped,
                    items = result
                });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    message = "An unexpected error occurred while syncing recording URLs to meetings.",
                    errors = new[] { ex.Message }
                });
            }
        }
    }
}
