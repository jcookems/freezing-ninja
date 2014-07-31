using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Xml.Linq;
using WhereIsMyMeeting2;

namespace Mapper.Parser
{
    static class SvgParser
    {
        //public static List<ElementDescription> foo(RoomInfo location)
        //{
        //    try
        //    {
        //        StreamResourceInfo xml = Application.GetResourceStream(new Uri(
        //            "Maps/" + location.Building + "-" + location.Floor + ".svg", UriKind.Relative));
        //        string svgDocContent = new StreamReader(xml.Stream).ReadToEnd();
        //        return foo(location, svgDocContent);
        //    }
        //    catch (Exception e)
        //    {
        //        throw new Exception("Could not load the map for " +
        //            "building '" + location.Building + "', " +
        //            "floor'" + location.Floor + "'\n" + e.Message);
        //    }
        //}

        public static List<ElementDescription> foo(RoomInfo location, string svgDocContent)
        {
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
                if (ret.IsHall)
                {
                    ret.Fill = "lightgray";
                }
                else if (styleParts["fill"] != "none")
                {
                    ret.Fill = styleParts["fill"];
                }
                ret.Path = PathInfo.ProcessPathData(node2.Attribute("d").Value);

                if (node2.Attribute("id") != null)
                {
                    ret.Text = node2.Attribute("id").Value;
                }

                ret.elementBounds = BoundingRectangle.GetBoundsFromPath(ret.Path);
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
