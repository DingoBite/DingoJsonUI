#if NEWTONSOFT_EXISTS
using System;
using System.Collections.Generic;
using System.Text;
using ImGuiNET;

namespace DingoJsonUI.GUI
{
    public sealed class UImGuiJsonSchemaDiagnosticsWindow
    {
        private IReadOnlyList<JsonUiSchemaDiagnostic> _diagnostics = Array.Empty<JsonUiSchemaDiagnostic>();

        public string WindowTitle { get; set; } = "Dingo Schema Validator";
        public bool DrawWhenValid { get; set; }
        public Action OnValidate { get; set; }
        public Action OnReloadAll { get; set; }

        public IReadOnlyList<JsonUiSchemaDiagnostic> Diagnostics
        {
            get => _diagnostics;
            set => _diagnostics = value ?? Array.Empty<JsonUiSchemaDiagnostic>();
        }

        public void Draw()
        {
            if (!DrawWhenValid && Diagnostics.Count == 0)
                return;

            ImGui.SetNextWindowSize(new System.Numerics.Vector2(520f, 220f), ImGuiCond.FirstUseEver);
            if (!ImGui.Begin(WindowTitle))
            {
                ImGui.End();
                return;
            }

            DrawToolbar();
            ImGui.Separator();
            DrawSummary();
            ImGui.Separator();
            DrawDiagnostics();

            ImGui.End();
        }

        private void DrawToolbar()
        {
            if (ImGui.SmallButton("Validate"))
                OnValidate?.Invoke();

            ImGui.SameLine();
            if (ImGui.SmallButton("Reload All"))
                OnReloadAll?.Invoke();

            ImGui.SameLine();
            if (ImGui.SmallButton("Copy"))
                ImGui.SetClipboardText(BuildClipboardText());
        }

        private void DrawSummary()
        {
            CountSeverities(out var errors, out var warnings, out var info);

            if (Diagnostics.Count == 0)
            {
                ImGui.TextColored(GetColor(JsonUiSchemaDiagnosticSeverity.Info), "No schema diagnostics.");
                return;
            }

            ImGui.TextColored(
                errors > 0 ? GetColor(JsonUiSchemaDiagnosticSeverity.Error) : GetColor(JsonUiSchemaDiagnosticSeverity.Info),
                $"Diagnostics: {Diagnostics.Count}  Errors: {errors}  Warnings: {warnings}  Info: {info}");
        }

        private void DrawDiagnostics()
        {
            if (Diagnostics.Count == 0)
                return;

            if (!ImGui.BeginChild("##diagnosticsList", new System.Numerics.Vector2(0f, 0f)))
            {
                ImGui.EndChild();
                return;
            }

            for (var i = 0; i < Diagnostics.Count; i++)
            {
                var diagnostic = Diagnostics[i];
                ImGui.PushID(i);
                ImGui.TextColored(GetColor(diagnostic.Severity), diagnostic.Severity.ToString());
                ImGui.SameLine(88f);
                ImGui.TextDisabled(diagnostic.SchemaPath);
                ImGui.PushTextWrapPos();
                ImGui.TextUnformatted(diagnostic.Message);
                ImGui.PopTextWrapPos();

                if (i + 1 < Diagnostics.Count)
                    ImGui.Separator();

                ImGui.PopID();
            }

            ImGui.EndChild();
        }

        private void CountSeverities(out int errors, out int warnings, out int info)
        {
            errors = 0;
            warnings = 0;
            info = 0;

            for (var i = 0; i < Diagnostics.Count; i++)
            {
                switch (Diagnostics[i].Severity)
                {
                    case JsonUiSchemaDiagnosticSeverity.Error:
                        errors++;
                        break;
                    case JsonUiSchemaDiagnosticSeverity.Warning:
                        warnings++;
                        break;
                    default:
                        info++;
                        break;
                }
            }
        }

        private string BuildClipboardText()
        {
            if (Diagnostics.Count == 0)
                return "No schema diagnostics.";

            var builder = new StringBuilder();
            for (var i = 0; i < Diagnostics.Count; i++)
                builder.AppendLine(Diagnostics[i].ToString());

            return builder.ToString();
        }

        private static System.Numerics.Vector4 GetColor(JsonUiSchemaDiagnosticSeverity severity)
        {
            return severity switch
            {
                JsonUiSchemaDiagnosticSeverity.Error => new System.Numerics.Vector4(1f, 0.35f, 0.25f, 1f),
                JsonUiSchemaDiagnosticSeverity.Warning => new System.Numerics.Vector4(1f, 0.75f, 0.25f, 1f),
                _ => new System.Numerics.Vector4(0.45f, 0.85f, 0.55f, 1f),
            };
        }
    }
}
#endif
