using System;
using System.Collections.Generic;
using System.Text;

namespace Requra.Infrastructure.Helpers
{
    public static class PipelineNodeMapper
    {
        public static string GetLabel(string nodeName)
        {
            if (string.IsNullOrWhiteSpace(nodeName))
                return "Queued for AI analysis";
            return nodeName.ToLower() switch
            {
                "queued" => "Queued for AI analysis",
                "detect_file_type" => "Detecting file type",
                "ingest" => "Uploading and preparing file",
                "ingest_route_after_ingest" => "Routing file for processing",

                "transcribe" => "Transcribing audio",
                "parse_to_chunks" => "Parsing content into chunks",
                "build_source_index" => "Building search index",
                "extract" => "Extracting requirements",
                "dedupe_requirements" => "Removing duplicate requirements",
                "retrieve_evidence" => "Retrieving supporting evidence",
                "classify" => "Classifying requirements",
                "evidence_grounding" => "Linking evidence to requirements",
                "generate" => "Generating AI output",
                "quality_gate" => "Validating output quality",
                "summarize" => "Summarizing results",
                "format" => "Formatting final output",

                "end" => "Processing completed",

                _ => "Processing..."
            };
        }
    }
}
