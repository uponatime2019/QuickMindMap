using System;
using System.Collections.Generic;

namespace QuickMindMap.Models
{
    public class MindMapTemplate
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Icon { get; set; } = "💡";
        public string Category { get; set; } = "General";
        public string Palette { get; set; } = "Vivid Spectrum";

        public Func<MindMapDocument> Factory { get; set; } = () => MindMapDocument.CreateDefault();

        public static readonly List<MindMapTemplate> Templates = new()
        {
            new MindMapTemplate
            {
                Id = "blank",
                Name = "Blank Mind Map",
                Description = "A clean slate ready for any idea or brainstorm.",
                Icon = "✨",
                Category = "General",
                Palette = "Vivid Spectrum",
                Factory = () => MindMapDocument.CreateDefault("Central Topic", "General", "Vivid Spectrum")
            },
            new MindMapTemplate
            {
                Id = "brainstorm",
                Name = "Brainstorming Session",
                Description = "Structure innovative ideas, causes, opportunities, and action items.",
                Icon = "🧠",
                Category = "Brainstorming",
                Palette = "Vivid Spectrum",
                Factory = () =>
                {
                    var doc = MindMapDocument.CreateDefault("Brainstorming Goal", "Brainstorming", "Vivid Spectrum");
                    var root = doc.RootNode;
                    var pal = ColorPalette.GetPalette(doc.ThemePalette);

                    var b1 = CreateChild(root, "Background & Causes", pal.BranchColors[0], "Right");
                    CreateChild(b1, "Current Pain Points", pal.BranchColors[0]);
                    CreateChild(b1, "Key Assumptions", pal.BranchColors[0]);

                    var b2 = CreateChild(root, "Creative Solutions", pal.BranchColors[1], "Right");
                    CreateChild(b2, "Quick Wins", pal.BranchColors[1]);
                    CreateChild(b2, "Moonshot Ideas", pal.BranchColors[1]);
                    CreateChild(b2, "Process Improvements", pal.BranchColors[1]);

                    var b3 = CreateChild(root, "Target Audience", pal.BranchColors[2], "Left");
                    CreateChild(b3, "Core Users", pal.BranchColors[2]);
                    CreateChild(b3, "Secondary Stakeholders", pal.BranchColors[2]);

                    var b4 = CreateChild(root, "Risks & Blockers", pal.BranchColors[3], "Left");
                    CreateChild(b4, "Technical Constraints", pal.BranchColors[3]);
                    CreateChild(b4, "Resource Limits", pal.BranchColors[3]);

                    var b5 = CreateChild(root, "Next Action Items", pal.BranchColors[4], "Right");
                    CreateChild(b5, "Assign Leads", pal.BranchColors[4]);
                    CreateChild(b5, "Schedule Review", pal.BranchColors[4]);

                    return doc;
                }
            },
            new MindMapTemplate
            {
                Id = "project_plan",
                Name = "Project Planning",
                Description = "Organize project scope, milestones, deliverables, and team responsibilities.",
                Icon = "🚀",
                Category = "Work",
                Palette = "Ocean Breeze",
                Factory = () =>
                {
                    var doc = MindMapDocument.CreateDefault("New Project Plan", "Work", "Ocean Breeze");
                    var root = doc.RootNode;
                    var pal = ColorPalette.GetPalette(doc.ThemePalette);

                    var b1 = CreateChild(root, "Project Scope", pal.BranchColors[0], "Right");
                    CreateChild(b1, "Objectives & Goals", pal.BranchColors[0]);
                    CreateChild(b1, "Deliverables", pal.BranchColors[0]);
                    CreateChild(b1, "Out of Scope", pal.BranchColors[0]);

                    var b2 = CreateChild(root, "Key Milestones", pal.BranchColors[1], "Right");
                    CreateChild(b2, "Phase 1: Research & Spec", pal.BranchColors[1]);
                    CreateChild(b2, "Phase 2: Development", pal.BranchColors[1]);
                    CreateChild(b2, "Phase 3: QA & Testing", pal.BranchColors[1]);
                    CreateChild(b2, "Phase 4: Launch", pal.BranchColors[1]);

                    var b3 = CreateChild(root, "Team & Roles", pal.BranchColors[2], "Left");
                    CreateChild(b3, "Project Lead", pal.BranchColors[2]);
                    CreateChild(b3, "Engineering Team", pal.BranchColors[2]);
                    CreateChild(b3, "Design & UX", pal.BranchColors[2]);

                    var b4 = CreateChild(root, "Budget & Resources", pal.BranchColors[3], "Left");
                    CreateChild(b4, "Software Licenses", pal.BranchColors[3]);
                    CreateChild(b4, "Infrastructure", pal.BranchColors[3]);

                    var b5 = CreateChild(root, "Risk Management", pal.BranchColors[4], "Right");
                    CreateChild(b5, "Contingency Plans", pal.BranchColors[4]);
                    CreateChild(b5, "Security & Compliance", pal.BranchColors[4]);

                    return doc;
                }
            },
            new MindMapTemplate
            {
                Id = "study_notes",
                Name = "Study & Exam Notes",
                Description = "Break down subjects into core concepts, formulas, examples, and questions.",
                Icon = "📚",
                Category = "Study",
                Palette = "Forest Bloom",
                Factory = () =>
                {
                    var doc = MindMapDocument.CreateDefault("Study Topic", "Study", "Forest Bloom");
                    var root = doc.RootNode;
                    var pal = ColorPalette.GetPalette(doc.ThemePalette);

                    var b1 = CreateChild(root, "Core Concepts", pal.BranchColors[0], "Right");
                    CreateChild(b1, "Definition 1", pal.BranchColors[0]);
                    CreateChild(b1, "Underlying Principles", pal.BranchColors[0]);
                    CreateChild(b1, "Historical Context", pal.BranchColors[0]);

                    var b2 = CreateChild(root, "Formulas & Rules", pal.BranchColors[1], "Right");
                    CreateChild(b2, "Key Theorem", pal.BranchColors[1]);
                    CreateChild(b2, "Step-by-Step Method", pal.BranchColors[1]);

                    var b3 = CreateChild(root, "Examples & Applications", pal.BranchColors[2], "Left");
                    CreateChild(b3, "Case Study A", pal.BranchColors[2]);
                    CreateChild(b3, "Practical Exercise", pal.BranchColors[2]);

                    var b4 = CreateChild(root, "Key Vocabulary", pal.BranchColors[3], "Left");
                    CreateChild(b4, "Terminology List", pal.BranchColors[3]);
                    CreateChild(b4, "Acronyms", pal.BranchColors[3]);

                    var b5 = CreateChild(root, "Exam Review Questions", pal.BranchColors[4], "Right");
                    CreateChild(b5, "Flashcard Prompts", pal.BranchColors[4]);
                    CreateChild(b5, "Practice Test", pal.BranchColors[4]);

                    return doc;
                }
            },
            new MindMapTemplate
            {
                Id = "pros_cons",
                Name = "Pros & Cons Decision",
                Description = "Evaluate complex decisions by weighing advantages, disadvantages, and mitigations.",
                Icon = "⚖️",
                Category = "Personal",
                Palette = "Sunset Glow",
                Factory = () =>
                {
                    var doc = MindMapDocument.CreateDefault("Decision to Make", "Personal", "Sunset Glow");
                    var root = doc.RootNode;
                    var pal = ColorPalette.GetPalette(doc.ThemePalette);

                    var b1 = CreateChild(root, "Advantages (Pros)", "#10B981", "Right");
                    CreateChild(b1, "Immediate Benefit", "#10B981");
                    CreateChild(b1, "Long-term Value", "#10B981");
                    CreateChild(b1, "Competitive Edge", "#10B981");

                    var b2 = CreateChild(root, "Disadvantages (Cons)", "#EF4444", "Left");
                    CreateChild(b2, "Financial Cost", "#EF4444");
                    CreateChild(b2, "Time Investment", "#EF4444");
                    CreateChild(b2, "Potential Risks", "#EF4444");

                    var b3 = CreateChild(root, "Mitigation Strategies", pal.BranchColors[2], "Right");
                    CreateChild(b3, "How to reduce costs", pal.BranchColors[2]);
                    CreateChild(b3, "Backup plan if failed", pal.BranchColors[2]);

                    var b4 = CreateChild(root, "Alternatives", pal.BranchColors[3], "Left");
                    CreateChild(b4, "Option B", pal.BranchColors[3]);
                    CreateChild(b4, "Maintain Status Quo", pal.BranchColors[3]);

                    var b5 = CreateChild(root, "Final Verdict", pal.BranchColors[4], "Right");
                    CreateChild(b5, "Agreed Course of Action", pal.BranchColors[4]);

                    return doc;
                }
            },
            new MindMapTemplate
            {
                Id = "meeting_notes",
                Name = "Meeting Notes & Actions",
                Description = "Track agenda items, discussions, decisions, and assigned responsibilities.",
                Icon = "📋",
                Category = "Work",
                Palette = "Royal Lavender",
                Factory = () =>
                {
                    var doc = MindMapDocument.CreateDefault("Team Meeting", "Work", "Royal Lavender");
                    var root = doc.RootNode;
                    var pal = ColorPalette.GetPalette(doc.ThemePalette);

                    var b1 = CreateChild(root, "Agenda & Goals", pal.BranchColors[0], "Right");
                    CreateChild(b1, "Goal 1: Status Sync", pal.BranchColors[0]);
                    CreateChild(b1, "Goal 2: Blocker Resolution", pal.BranchColors[0]);

                    var b2 = CreateChild(root, "Discussion Topics", pal.BranchColors[1], "Right");
                    CreateChild(b2, "Product Roadmap Updates", pal.BranchColors[1]);
                    CreateChild(b2, "Customer Feedback", pal.BranchColors[1]);

                    var b3 = CreateChild(root, "Attendees & Roles", pal.BranchColors[2], "Left");
                    CreateChild(b3, "Chair / Host", pal.BranchColors[2]);
                    CreateChild(b3, "Note Taker", pal.BranchColors[2]);
                    CreateChild(b3, "Participants", pal.BranchColors[2]);

                    var b4 = CreateChild(root, "Key Decisions Made", pal.BranchColors[3], "Left");
                    CreateChild(b4, "Approved feature release date", pal.BranchColors[3]);
                    CreateChild(b4, "Budget sign-off", pal.BranchColors[3]);

                    var b5 = CreateChild(root, "Action Items", pal.BranchColors[4], "Right");
                    CreateChild(b5, "Prepare presentation deck (Due Fri)", pal.BranchColors[4]);
                    CreateChild(b5, "Send summary email to team", pal.BranchColors[4]);

                    return doc;
                }
            },
            new MindMapTemplate
            {
                Id = "swot",
                Name = "SWOT Analysis",
                Description = "Analyze Strengths, Weaknesses, Opportunities, and Threats for strategy.",
                Icon = "📊",
                Category = "Brainstorming",
                Palette = "Vivid Spectrum",
                Factory = () =>
                {
                    var doc = MindMapDocument.CreateDefault("Business / Product SWOT", "Brainstorming", "Vivid Spectrum");
                    var root = doc.RootNode;

                    var b1 = CreateChild(root, "Strengths (Internal)", "#10B981", "Right");
                    CreateChild(b1, "Unique technology / IP", "#10B981");
                    CreateChild(b1, "Strong brand recognition", "#10B981");
                    CreateChild(b1, "Talented & agile team", "#10B981");

                    var b2 = CreateChild(root, "Opportunities (External)", "#0284C7", "Right");
                    CreateChild(b2, "Expanding market demand", "#0284C7");
                    CreateChild(b2, "Strategic partnerships", "#0284C7");
                    CreateChild(b2, "New customer segments", "#0284C7");

                    var b3 = CreateChild(root, "Weaknesses (Internal)", "#F59E0B", "Left");
                    CreateChild(b3, "Limited marketing budget", "#F59E0B");
                    CreateChild(b3, "Dependency on single supplier", "#F59E0B");

                    var b4 = CreateChild(root, "Threats (External)", "#EF4444", "Left");
                    CreateChild(b4, "Emerging competitors", "#EF4444");
                    CreateChild(b4, "Changing regulations", "#EF4444");

                    return doc;
                }
            }
        };

        private static MindMapNode CreateChild(MindMapNode parent, string text, string branchColor, string side = "Auto")
        {
            var node = new MindMapNode
            {
                Id = Guid.NewGuid().ToString("N"),
                ParentId = parent.Id,
                Text = text,
                IsRoot = false,
                BranchColor = branchColor,
                FillColor = null,
                TextColor = null,
                FontSize = parent.IsRoot ? 14 : 13,
                FontWeight = parent.IsRoot ? "SemiBold" : "Normal",
                ShapeType = "RoundedRectangle",
                Side = side,
                Width = 130,
                Height = 36,
                Children = new List<MindMapNode>()
            };
            parent.Children.Add(node);
            return node;
        }
    }
}
