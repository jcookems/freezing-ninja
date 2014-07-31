using Mapper.Consumer;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Xml.Linq;

namespace Mapper.Parser
{
    public static class SvgParser
    {
        public static List<ElementDescription> ParseSvgDoc(RoomInfo location, string rawContent)
        {
            int ind = rawContent.IndexOf(" id='thesvg'");
            if (ind > 0)
            {
                rawContent = "<svg " + rawContent.Substring(ind);
            }
            ind = rawContent.IndexOf(" id=\"thesvg\"");
            if (ind > 0)
            {
                rawContent = "<svg " + rawContent.Substring(ind);
            }

            // Now do some hacky junk
            string svgDocContent = FixClosingTags(rawContent);

            var elements = new List<ElementDescription>();
            XElement doc = null;
            try
            {
                doc = XDocument.Parse(svgDocContent).Root;
            }
            catch (Exception e)
            {
                throw new Exception("Could not load the map for " +
                    "building '" + location.Building + "', " +
                    "floor'" + location.Floor + "'\n" + e.Message);
            }

            foreach (XElement node in doc.Nodes().OfType<XElement>())
            {
                foreach (XElement node2 in node.Nodes().OfType<XElement>())
                {
                    elements.Add(ParseSvgElement(node2, node.Attribute("id").Value));
                }
            }

            elements = elements.Where(p => p != null).ToList();
            return elements;
        }

        private static string FixClosingTags(string svgDocContent)
        {
            int mode = 0;
            bool prevIsOpenAngle = false;
            bool prevIsSlash = false;
            Stack<string> items = new Stack<string>();
            string currThing = "";
            for (int i = 0; i < svgDocContent.Length; i++)
            {
                Char cur = svgDocContent[i];

                if (currThing != "")
                {
                    if (Char.IsLetterOrDigit(cur) || cur == ':')
                    {
                        currThing += cur;
                    }
                    else
                    {
                        items.Push(currThing.TrimStart('/'));
                        currThing = "";
                    }
                }

                if (prevIsOpenAngle)
                {
                    mode =  (cur == '/' ? 2 : 1);
                    currThing = "" + cur;
                }
                else if (cur == '>' && prevIsSlash)
                {
                    mode = 3;
                }

                if (cur == '>')
                {
                    string lastname = (items.Count == 0 ? null : items.Pop());

                    if (mode == 1)
                    {
                        items.Push(lastname);
                    }
                    else if (mode == 2)
                    {
                        var opening = (items.Count == 0 ? "XXX" : items.Pop());
                        if (opening != lastname)
                        {
                            string ret = svgDocContent.Substring(0, i - 2 - lastname.Length);
                            if (opening != "XXX") ret +=                                 "</" + opening + ">";
                            while (items.Count > 0)
                            {
                                ret += "</" + items.Pop() + ">";
                            }
                            return ret;
                        }
                    }
                }

                prevIsOpenAngle = (cur == '<');
                prevIsSlash = (cur == '/');
            }

            return svgDocContent;
        }

        public static ElementDescription ParseSvgElement(XElement node2, String parentName)
        {
            ElementDescription ret = new ElementDescription();
            string name = node2.Name.LocalName;

            if (parentName.ToLowerInvariant() == "rooms".ToLowerInvariant())
            {
                ret.Parent = ParentType.Rooms;
            }
            else if (parentName.ToLowerInvariant() == "labels".ToLowerInvariant())
            {
                ret.Parent = ParentType.Labels;
            }
            else if (parentName.ToLowerInvariant() == "background".ToLowerInvariant())
            {
                ret.Parent = ParentType.Background;
            }
            else if (parentName.ToLowerInvariant() == "tooltip".ToLowerInvariant())
            {
                return null;
            }
            else
            {
                throw new ArgumentOutOfRangeException("Unexpected node type: " + parentName);
            }

            var styleParts = new Dictionary<string, string>();
            // Fill out some defaults
            styleParts["stroke"] = "black";
            styleParts["font-size"] = "11";
            styleParts["fill"] = "none";

            if (node2.Attribute("style") != null)
            {
                foreach (var part in node2.Attribute("style").Value.Split(new char[] { ';' }, System.StringSplitOptions.RemoveEmptyEntries).Select(p => p.Trim()).Select(p => p.Split(':')))
                {
                    styleParts[part[0]] = part[1];
                }
            }

            if (name == "circle")
            {
                ret.Type = ElementType.Circle;
                ret.Width = double.Parse(node2.Attribute("r").Value) * 2;
                ret.Height = double.Parse(node2.Attribute("r").Value) * 2;
                ret.Fill = styleParts["fill"];
                ret.Stroke = styleParts["stroke"];
                ret.CenterX = double.Parse(node2.Attribute("cx").Value);
                ret.CenterY = double.Parse(node2.Attribute("cy").Value);
            }
            else if (name == "path")
            {
                ret.Type = ElementType.Path;
                ret.IsHall = (node2.Attribute("desc") == null ? false : node2.Attribute("desc").Value.Contains("irculation"));
                ret.Stroke = styleParts["stroke"];
                if (ret.IsHall.Value)
                {
                    ret.Fill = "lightgray";
                }
                else if (styleParts["fill"] != "none")
                {
                    ret.Fill = styleParts["fill"];
                }
                ret.Path = node2.Attribute("d").Value;

                if (node2.Attribute("id") != null)
                {
                    ret.Text = node2.Attribute("id").Value;
                }

                ret.elementBounds = BoundingRectangle.GetBoundsFromPath(ret.GetPath());
            }
            else if (name == "text")
            {
                ret.Type = ElementType.Text;
                ret.CenterX = double.Parse(node2.Attribute("x").Value);
                ret.CenterY = double.Parse(node2.Attribute("y").Value);
                ret.Stroke = styleParts["stroke"];
                ret.FontSize = double.Parse(styleParts["font-size"]);
                ret.Text = node2.Value;
            }
            else
            {
                throw new InvalidOperationException("Unexpected node: " + name);
            }

            return ret;
        }
    }
}
