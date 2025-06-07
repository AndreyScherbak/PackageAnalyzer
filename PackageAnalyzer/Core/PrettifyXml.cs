using System.Windows.Documents;
using System.Windows.Media;

namespace PackageAnalyzer.Core
{
    internal class PrettifyXml
    {
        internal Paragraph DisplayXmlWithHighlighting(string xml)
        {
            // Create a single paragraph
            Paragraph paragraph = new Paragraph();

            // Define colors for syntax highlighting
            Brush tagColor = Brushes.Blue;
            Brush attributeNameColor = Brushes.Red;
            Brush attributeValueColor = Brushes.Brown;
            Brush textColor = Brushes.Black;

            int currentIndex = 0;
            while (currentIndex < xml.Length)
            {
                int nextTagOpen = xml.IndexOf('<', currentIndex);
                if (nextTagOpen < 0)
                {
                    AppendText(paragraph, xml.Substring(currentIndex), textColor);
                    break;
                }

                // Add the text before the tag
                if (nextTagOpen > currentIndex)
                {
                    AppendText(paragraph, xml.Substring(currentIndex, nextTagOpen - currentIndex), textColor);
                }

                // Find the end of the tag
                int nextTagClose = xml.IndexOf('>', nextTagOpen);
                if (nextTagClose < 0) break;

                // Extract the tag content
                string tagContent = xml.Substring(nextTagOpen, nextTagClose - nextTagOpen + 1);

                // Color the tag and its attributes
                AppendText(paragraph, "<", tagColor);
                int spaceIndex = tagContent.IndexOf(' ');
                int endIndex = tagContent.IndexOf('>');

                if (spaceIndex < 0 || spaceIndex > endIndex)
                {
                    // Handle tag with no attributes
                    AppendText(paragraph, tagContent.Substring(1, endIndex - 1), tagColor);
                    AppendText(paragraph, ">", tagColor);
                }
                else
                {
                    // Add the tag name
                    AppendText(paragraph, tagContent.Substring(1, spaceIndex - 1), tagColor);

                    // Process and color the attributes
                    string attributePart = tagContent.Substring(spaceIndex + 1, endIndex - spaceIndex - 1);
                    while (!string.IsNullOrEmpty(attributePart))
                    {
                        // Find the next attribute-value pair
                        int equalsIndex = attributePart.IndexOf('=');
                        if (equalsIndex < 0) break;

                        string attrName = attributePart.Substring(0, equalsIndex).Trim();
                        AppendText(paragraph, " ", tagColor); // Add space before attribute name
                        AppendText(paragraph, attrName, attributeNameColor);

                        // Find the start and end of the attribute value
                        int quoteStart = attributePart.IndexOf('"', equalsIndex + 1);
                        int quoteEnd = attributePart.IndexOf('"', quoteStart + 1);
                        if (quoteStart < 0 || quoteEnd < 0) break;

                        string attrValue = attributePart.Substring(quoteStart, quoteEnd - quoteStart + 1).Trim();
                        AppendText(paragraph, "=", attributeNameColor);
                        AppendText(paragraph, attrValue, attributeValueColor);

                        // Move to the next attribute
                        attributePart = attributePart.Substring(quoteEnd + 1).Trim();
                    }

                    // Check if the tag is self-closing
                    if (tagContent.EndsWith("/>"))
                    {
                        AppendText(paragraph, " /", tagColor); // Add space and slash for self-closing tags
                    }

                    AppendText(paragraph, ">", tagColor);
                }

                currentIndex = nextTagClose + 1;
            }
            return paragraph;
        }

        private void AppendText(Paragraph paragraph, string text, Brush color)
        {
            Run run = new Run(text) { Foreground = color };
            paragraph.Inlines.Add(run);
        }
    }
}
