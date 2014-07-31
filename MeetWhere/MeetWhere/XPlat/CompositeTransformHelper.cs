#if !NETFX_CORE
using System.Windows.Media;
#else
using Windows.UI.Xaml.Media;
#endif
using System.Diagnostics;

namespace MeetWhere.XPlat
{
    public class CompositeTransformHelper
    {
#if NETFX_COREc || WINDOWS_PHONE
        private CompositeTransform t = new CompositeTransform();
#else
            private TransformGroup t = new TransformGroup();
            private ScaleTransform st = new ScaleTransform();
            // SkewTransform());
            private RotateTransform rt = new RotateTransform();
            private TranslateTransform tt = new TranslateTransform();
#endif

        public CompositeTransformHelper()
        {
#if !NETFX_COREc && !WINDOWS_PHONE
                this.t.Children.Add(st);
                this.t.Children.Add(rt);
                this.t.Children.Add(tt);
#endif
        }

        public Transform Transform { get { return this.t; } }

        public double CenterX
        {
            get
            {
#if NETFX_COREc || WINDOWS_PHONE
                return this.t.CenterX;
#else
                    return this.rt.CenterX;
#endif
            }
            set
            {
#if NETFX_COREc || WINDOWS_PHONE
                this.t.CenterX = value;
#else
                    this.rt.CenterX = value;
                    this.st.CenterX = value;
#endif
            }
        }
        public double CenterY
        {
            get
            {
#if NETFX_COREc || WINDOWS_PHONE
                return this.t.CenterY;
#else
                    return this.rt.CenterY;
#endif
            }
            set
            {
#if NETFX_COREc || WINDOWS_PHONE
                this.t.CenterY = value;
#else
                    this.rt.CenterY = value;
                    this.st.CenterY = value;
#endif
            }
        }

        public double Rotation
        {
            get
            {
#if NETFX_COREc || WINDOWS_PHONE
                return this.t.Rotation;
#else
                    return this.rt.Angle;
#endif
            }
            set
            {
#if NETFX_COREc || WINDOWS_PHONE
                this.t.Rotation = value;
#else
                    this.rt.Angle = value;
#endif
            }
        }

        //public double ScaleX { get; set; }
        public double Scale
        {
            get
            {
#if NETFX_COREc || WINDOWS_PHONE
                return this.t.ScaleX;
#else
                    return this.st.ScaleX;
#endif
            }
            set
            {
#if NETFX_COREc || WINDOWS_PHONE
                this.t.ScaleX = value;
                this.t.ScaleY = value;
#else
                    this.st.ScaleX = value;
                    this.st.ScaleY = value;
#endif
            }
        }

        //public double SkewX { get; set; }
        //public double SkewY { get; set; }
        public double TranslateX
        {
            get
            {
#if NETFX_COREc || WINDOWS_PHONE
                return this.t.TranslateX;
#else
                    return this.tt.X;
#endif
            }
            set
            {
#if NETFX_COREc || WINDOWS_PHONE
                this.t.TranslateX = value;
#else
                    this.tt.X = value;
#endif
            }
        }

        public double TranslateY
        {
            get
            {
#if NETFX_COREc || WINDOWS_PHONE
                return this.t.TranslateY;
#else
                    return this.tt.Y;
#endif
            }
            set
            {
#if NETFX_COREc || WINDOWS_PHONE
                this.t.TranslateY = value;
#else
                    this.tt.Y = value;
#endif
            }
        }

        public  GeneralTransform Inverse
        {
            get
            {
#if NETFX_COREc || WINDOWS_PHONE
                return this.t.Inverse;
#else
                var ret = new TransformGroup();
                ret.Children.Add((Transform)tt.Inverse);
                ret.Children.Add((Transform)rt.Inverse);
                ret.Children.Add((Transform)st.Inverse);
                return ret;
#endif
            }
        }

    }

}
