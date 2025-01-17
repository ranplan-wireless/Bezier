﻿using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Numerics;

namespace System.Geometry
{
    /// <summary>
    /// Represents Bezier Curve.
    /// </summary>
    public class Bezier : IPathShape, IInterval
    {
        internal readonly Vector2[] points;
        internal readonly int order;
        private bool _linear;
        internal float _t1 = 0;
        internal float _t2 = 1;
        private List<List<Vector2>> dpoints;
        private bool clockwise;



        internal Interval Interval;

        Interval IInterval.Interval
        {
            get => Interval;
            set => Interval = value;
        }

        /// <summary>
        /// Gets the points of the curve.
        /// </summary>
        public IReadOnlyList<Vector2> Points
        {
            get
            {
                return points;
            }
        }

        /// <summary>
        /// Creates a Cubic Bezier Curve.
        /// </summary>
        public Bezier(Vector2 p1, Vector2 p2, Vector2 p3, Vector2 p4)
        {
            points = new[] { p1, p2, p3, p4 };
            order = points.Length - 1;

            CheckLinear(this);


            _t1 = 0;
            _t2 = 1;

            Update();
        }

        /// <summary>
        /// Creates a Quadratic Bezier Curve.
        /// </summary>
        public Bezier(Vector2 p1, Vector2 p2, Vector2 p3)
        {
            points = new[] { p1, p2, p3 };
            order = points.Length - 1;

            CheckLinear(this);

            _t1 = 0;
            _t2 = 1;

            Update();
        }

        internal float[] getInterval()
        {
            throw new NotImplementedException();
        }

        internal double getCurveLength()
        {
            throw new NotImplementedException();
        }


        /// <summary>
        /// Constructs a curve along a straight line.
        /// </summary>
        /// <param name="p1">Start of the line.</param>
        /// <param name="p2">End of the line.</param>
        public Bezier(Vector2 p1, Vector2 p2)
        {
            float x1 = p1.X;
            float y1 = p1.Y;
            float x2 = p2.X;
            float y2 = p2.Y;

            float dx = (x2 - x1) / 3;
            float dy = (y2 - y1) / 3;

            points = new[] { p1, new Vector2(x1 + dx, y1 + dy), new Vector2(x1 + 2 * dx, y1 + 2 * dy), p2 };
            order = points.Length - 1;

            CheckLinear(this);

            _t1 = 0;
            _t2 = 1;

            Update();
        }

        /// <summary>
        /// Creates a Bezier Curve.
        /// </summary>
        public Bezier(Vector2[] points)
        {
            if (points.Length < 2)
            {
                throw new ArgumentException();
            }

            if (points.Length == 2)
            {
                Vector2 p1 = points[0];
                Vector2 p2 = points[1];

                float x1 = p1.X;
                float y1 = p1.Y;
                float x2 = p2.X;
                float y2 = p2.Y;

                float dx = (x2 - x1) / 3;
                float dy = (y2 - y1) / 3;

                this.points = new[] { p1, new Vector2(x1 + dx, y1 + dy), new Vector2(x1 + 2 * dx, y1 + 2 * dy), p2 };
            }
            else
            {
                this.points = new Vector2[points.Length];
                points.CopyTo(this.points, 0);
            }

            order = points.Length - 1;

            CheckLinear(this);

            _t1 = 0;
            _t2 = 1;

            Update();
        }


        /// <summary>
        /// Creates a Bezier Curve.
        /// </summary>
        public Bezier(IList<Vector2> points)
        {
            if (points.Count < 2 || points.Count > 4)
            {
                throw new ArgumentException();
            }

            if (points.Count == 2)
            {
                Vector2 p1 = points[0];
                Vector2 p2 = points[1];

                float x1 = p1.X;
                float y1 = p1.Y;
                float x2 = p2.X;
                float y2 = p2.Y;

                float dx = (x2 - x1) / 3;
                float dy = (y2 - y1) / 3;

                points = new[] { p1, new Vector2(x1 + dx, y1 + dy), new Vector2(x1 + 2 * dx, y1 + 2 * dy), p2 };
            }
            else
            {
                this.points = new Vector2[points.Count];
                points.CopyTo(this.points, 0);
            }

            order = points.Count - 1;

            CheckLinear(this);

            _t1 = 0;
            _t2 = 1;

            Update();
        }


        private void Update()
        {
            ComputeDerivativeCoordinates();

            ComputeDirection();
        }

        private void ComputeDerivativeCoordinates()
        {
            // one-time compute derivative coordinates
            dpoints = new List<List<Vector2>>();

            List<Vector2> p = new List<Vector2>(points);
            int d = p.Count;
            int c = d - 1;

            for (; d > 1; d--, c--)
            {
                List<Vector2> list = new List<Vector2>();
                for (int j = 0; j < c; j++)
                {
                    var dpt = c * (p[j + 1] - p[j]);

                    list.Add(dpt);
                }

                dpoints.Add(list);
                p = list;
            }
        }

        private void ComputeDirection()
        {
            Vector2[] points = this.points;
            float angle = Utils.Angle(points[0], points[order], points[1]);

            clockwise = angle > 0;
        }

        /// <summary>
        /// Calculates the length of this Bezier curve.
        /// Length is calculated using numerical approximation,
        /// specifically the Legendre-Gauss quadrature algorithm.
        /// </summary>
        public float Length
        {
            get
            {
                return Utils.Length(Tangent);
            }
        }

        /// <summary>
        /// Calculates a point on the curve, for a given t value between 0 and 1 (inclusive).
        /// </summary>
        public Vector2 Position(float t)
        {
            // shortcuts
            if (Utils.Approximately(t, 0))
            {
                return points[0];
            }
            if (Utils.Approximately(t, 1))
            {
                return points[order];
            }

            Vector2[] p = points;
            float mt = 1 - t;

            // linear?
            if (order == 1)
            {
                Vector2 ret = new Vector2(x: mt * p[0].X + t * p[1].X, y: mt * p[0].Y + t * p[1].Y);
                return ret;
            }

            // quadratic/cubic curve?
            if (order < 4)
            {
                float mt2 = mt * mt;
                float t2 = t * t;
                float a = 0f;
                float b = 0f;
                float c = 0f;
                float d = 0f;
                if (order == 2)
                {
                    p = new[] { p[0], p[1], p[2], Vector2.Zero };
                    a = mt2;
                    b = mt * t * 2;
                    c = t2;
                }
                else if (order == 3)
                {
                    a = mt2 * mt;
                    b = mt2 * t * 3;
                    c = mt * t2 * 3;
                    d = t * t2;
                }

                return a * p[0] + b * p[1] + c * p[2] + d * p[3];
            }

            // higher order curves: use de Casteljau's computation
            List<Vector2> dCpts = new List<Vector2>(points);
            while (dCpts.Count > 1)
            {
                for (int i = 0; i < dCpts.Count - 1; i++)
                {
                    dCpts[i] = dCpts[i] + (dCpts[i + 1] - dCpts[i]) * t;
                }
                dCpts.RemoveAt(dCpts.Count - 1);
            }

            return dCpts[0];
        }

        /// <summary>
        /// Calculates the curve tangent at the specified t value. Note that this yields a not-normalized vector {x: dx, y: dy}.
        /// </summary>
        public Vector2 Tangent(float t)
        {
            float mt = 1 - t;
            List<Vector2> p = dpoints[0];

            float a = 0;
            float b = 0;
            float c = 0;

            if (order == 2)
            {
                p = new List<Vector2> { p[0], p[1], Vector2.Zero };
                a = mt;
                b = t;
            }
            if (order == 3)
            {
                a = mt * mt;
                b = mt * t * 2;
                c = t * t;
            }

            return a * p[0] + b * p[1] + c * p[2];
        }

        /// <summary>
        /// Calculates the curve normal at the specified t value. Note that this yields a normalized vector {x: nx, y: ny}.
        /// </summary>
        public Vector2 Normal(float t)
        {
            Vector2 d = Tangent(t);
            float q = sqrt(d.X * d.X + d.Y * d.Y);
            return new Vector2(x: -d.Y / q, y: d.X / q);
        }


        /// <summary>
        /// Raises the Bezier curve.
        /// </summary>
        /// <returns></returns>
        public Bezier Raise()
        {
            Vector2[] p = points;
            List<Vector2> np = new List<Vector2> { p[0] };
            int k = p.Length;

            for (int i = 1; i < k; i++)
            {
                Vector2 pi = p[i];
                Vector2 pim = p[i - 1];
                np.Add(new Vector2(x: (k - i) / k * pi.X + i / k * pim.X, y: (k - i) / k * pi.Y + i / k * pim.Y));
            }

            np[k] = p[k - 1];

            if (np.Count == 3)
            {
                return new Bezier(np[0], np[1], np[2]);
            }
            else if (np.Count == 4)
            {
                return new Bezier(np[0], np[1], np[2], np[3]);
            }

            throw new Exception();
        }

        /// <summary>
        /// Generates all hull points, at all iterations, for an on-curve point at the specified t-value.
        /// For quadratic curves, this generates a point[6], and for cubic curves, this generates a point[10],
        /// where the first iteration is [0,1,2] and [0,1,2,3] respectively, the second iteration is [3,4]
        /// and [4,5,6] respectively, the third iteration is [5] (the on-curve point for quadratic curves)
        /// and [7,8] respectively, and the fourth iteration (for cubic curves only) is [9].
        /// </summary>
        public List<Vector2> Hull(float t)
        {
            List<Vector2> p = new List<Vector2>(points);
            List<Vector2> q = new List<Vector2>();

            q.Add(p[0]);
            q.Add(p[1]);
            q.Add(p[2]);

            if (order == 3)
            {
                q.Add(p[3]);
            }

            // we lerp between all points at each iteration, until we have 1 point left.
            while (p.Count > 1)
            {
                List<Vector2> _p = new List<Vector2>();
                for (int i = 0; i < p.Count - 1; i++)
                {
                    Vector2 pt = Utils.Lerp(t, p[i], p[i + 1]);
                    q.Add(pt);
                    _p.Add(pt);
                }
                p = _p;
            }

            return q;
        }

        /// <summary>
        /// Splits a curve at t into two new curves that together are equivalent to the original curve.
        /// </summary>
        public Split Split(float t)
        {
            // no shortcut: use "de Casteljau" iteration.
            List<Vector2> q = Hull(t);
            Split result = new Split
            {
                Left =
          order == 2
            ? new Bezier(q[0], q[3], q[5])
            : new Bezier(q[0], q[4], q[7], q[9]),
                Right =
            order == 2
              ? new Bezier(q[5], q[4], q[2])
            : new Bezier(q[9], q[8], q[6], q[3]),
                Span = q
            };

            // make sure we bind _t1/_t2 information!
            result.Left._t1 = Utils.Map(0, 0, 1, _t1, _t2);
            result.Left._t2 = Utils.Map(t, 0, 1, _t1, _t2);
            result.Right._t1 = Utils.Map(t, 0, 1, _t1, _t2);
            result.Right._t2 = Utils.Map(1, 0, 1, _t1, _t2);

            return result;
        }

        public Pair<Bezier> Break(float t)
        {
            var s = Split(t);
            return new Pair<Bezier>(s.Left, s.Right);
        }

        Pair<IPathShape> IPathShape.Break(float t)
        {
            var b = Break(t);
            if (b == null) return null;
            return new Pair<IPathShape>(b.Left, b.Right);
        }

        public Pair<Bezier> BreakAtPoint(Vector2 pointOfBreak)
        {
            throw new NotImplementedException();
        }

        Pair<IPathShape> IPathShape.BreakAtPoint(Vector2 pointOfBreak)
        {
            var b = BreakAtPoint(pointOfBreak);
            if (b == null) return null;
            return new Pair<IPathShape>(b.Left, b.Right);
        }

        public Bezier Clone()
        {
            return new Bezier(new List<Vector2>(points));
        }

        IPathShape IPathShape.Clone()
        {
            return Clone();
        }

        /// <summary>
        /// Splits the curve on t1, after which the resulting second subcurve is split on (a scaled) t2, yielding a new curve that is equivalent to the original curve over the interval [t1, t2].
        /// </summary>
        public Bezier Split(float t1, float t2)
        {
            // shortcuts
            if (t1 == 0 && t2 != 0)
            {
                return Split(t2).Left;
            }
            if (t2 == 1)
            {
                return Split(t1).Right;
            }

            // no shortcut: use "de Casteljau" iteration.
            List<Vector2> q = Hull(t1);

            Split result = new Split
            {
                Left = order == 2 ? new Bezier(q[0], q[3], q[5]) : new Bezier(q[0], q[4], q[7], q[9]),
                Right = order == 2 ? new Bezier(q[5], q[4], q[2]) : new Bezier(q[9], q[8], q[6], q[3]),
                Span = q
            };

            // make sure we bind _t1/_t2 information!
            result.Left._t1 = Utils.Map(0, 0, 1, _t1, _t2);
            result.Left._t2 = Utils.Map(t1, 0, 1, _t1, _t2);
            result.Right._t1 = Utils.Map(t1, 0, 1, _t1, _t2);
            result.Right._t2 = Utils.Map(1, 0, 1, _t1, _t2);

            // if we have a t2, split again:
            t2 = Utils.Map(t2, t1, 1, 0, 1);
            Split subsplit = result.Right.Split(t2);
            return subsplit.Left;
        }

        /// <summary>
        /// Calculates all the extrema on a curve. Extrema are calculated for each dimension,
        /// rather than for the full curve, so that the result is not the number of convex/concave transitions,
        /// but the number of those transitions for each separate dimension.
        /// 
        /// This function yields an object {x: [num, num, ...], y: [num, num, ...], values: [...]} where each
        /// dimension lists the array of t values at which an extremum occurs, and the values property is the aggregate of the t values across all dimensions.
        /// 
        /// These points can be used to determine the reach of a curve.
        /// </summary>
        /// <returns>Returns extrema.</returns>
        public Extrema Extrema()
        {
            List<float> roots = new List<float>();

            IEnumerable<float> px = dpoints[0].Select(pxx => pxx.X);
            IEnumerable<float> py = dpoints[0].Select(pxx => pxx.Y);

            float[] resultx = Utils.Droots(px.ToArray());
            float[] resulty = Utils.Droots(py.ToArray());

            if (order == 3)
            {
                px = dpoints[1].Select(pxx => pxx.X);
                py = dpoints[1].Select(pxx => pxx.Y);

                resultx = resultx.Concat(Utils.Droots(px.ToArray())).ToArray();
                resulty = resulty.Concat(Utils.Droots(py.ToArray())).ToArray();
            }

            resultx = resultx.Where((t) => Utils.Between(t, 0, 1)).ToArray();
            resulty = resulty.Where((t) => Utils.Between(t, 0, 1)).ToArray();

            List<float> rx = resultx.ToList();
            List<float> ry = resulty.ToList();

            rx.Sort(Utils.NumberSort);
            ry.Sort(Utils.NumberSort);

            return new Extrema
            {
                X = rx.ToArray(),
                Y = ry.ToArray(),
                Values = rx.Union(ry).OrderBy(x => x).ToArray()
            };
        }

        /// <summary>
        /// Calculates the bounding box for this curve, based on its hull coordinates and its extrema.
        /// </summary>
        /// <returns>Returns the bounding box.</returns>
        public BoundingBox BoundingBox
        {
            get
            {
                Extrema extrema = Extrema();

                Utils.GetMinMaxX(this, extrema.X, out float minx, out float maxx);
                Utils.GetMinMaxY(this, extrema.Y, out float miny, out float maxy);

                return new BoundingBox(new Vector2(minx, miny), new Vector2(maxx, maxy));
            }
        }

        /// <summary>
        /// Gets weather the two curves overlap (intersect).
        /// </summary>
        /// <param name="curve1">Curve 1</param>
        /// <param name="curve2">Curve 2</param>
        /// <returns>Returns True the the two intersect.</returns>
        public static bool Overlaps(Bezier curve1, Bezier curve2)
        {
            BoundingBox lbbox = curve1.BoundingBox;
            BoundingBox tbbox = curve2.BoundingBox;

            return Geometry.BoundingBox.Intersects(lbbox, tbbox);
        }

        /// <summary>
        /// Returns a point on the curve at t=, offset along its normal by a distance d.
        /// </summary>
        /// <param name="t"></param>
        /// <param name="d"></param>
        /// <returns></returns>
        public Vector2 Offset(float t, float d)
        {
            var c = this.Position(t);
            var n = this.Normal(t);

            return c + n * d;
        }

        public Vector2 Offset(float t, float d, out Vector2 c, out Vector2 n)
        {
            c = this.Position(t);
            n = this.Normal(t);

            return c + n * d;
        }

        /// <summary>
        /// Creates a new curve, offset along the curve normals, at distance d.
        /// Note that deep magic lies here and the offset curve of a Bezier curve cannot ever be another Bezier curve.
        /// As such, this function "cheats" and yields an array of curves which, taken together, form a single continuous curve equivalent to what a theoretical offset curve would be.
        /// </summary>
        /// <param name="d"></param>
        /// <returns></returns>
        public List<Bezier> Offset(float d)
        {
            if (this._linear)
            {
                var nv = this.Normal(0);
                var coords = this.points.Select(p =>
                {
                    var ret = new Vector2
                    {
                        X = p.X + d * nv.X,
                        Y = p.Y + d * nv.Y
                    };

                    return ret;
                });

                return new List<Bezier> { new Bezier(coords.ToArray()) };
            }

            var reduced = this.Reduce();
            return reduced.Select(s =>
            {
                return s.Scale(d);
            }).ToList();
        }

        /// <summary>
        /// Gets whether the curve is "simple" or not.
        /// </summary>
        public bool IsSimple()
        {
            if (order == 3)
            {
                float a1 = Utils.Angle(points[0], points[3], points[1]);
                float a2 = Utils.Angle(points[0], points[3], points[2]);
                if ((a1 > 0 && a2 < 0) || (a1 < 0 && a2 > 0))
                {
                    return false;
                }
            }

            Vector2 n1 = Normal(0);
            Vector2 n2 = Normal(1);

            float s = n1.X * n2.X + n1.Y * n2.Y;

            float angle = abs(acos(s));
            return angle < pi / 3;
        }

        /// <summary>
        /// This generates a curve's outline at distance d along the curve normal and anti-normal. The result is an array of curves that taken together form the outline path for this curve. The caps are cubic beziers with the control points oriented to form a straight line.
        /// </summary>
        /// <param name="d">Distance.</param>
        /// <returns>A PolyBezier corresponding with the outline of the bezier curve.</returns>
        public PolyBezier Outline(float d)
        {
            return Outline(d, d);
        }

        public PolyBezier Outline(float d1, float d2)
        {
            var reduced = this.Reduce();
            var len = reduced.Count;
            var fcurves = new List<Bezier>();
            var bcurves = new List<Bezier>();

            var alen = 0.0f;
            var tlen = this.Length;

            // form curve oulines
            foreach (var segment in reduced)
            {
                var slen = segment.Length;

                fcurves.Add(segment.Scale(d1));
                bcurves.Add(segment.Scale(-d2));

                alen += slen;
            }

            // reverse the "return" outline
            bcurves = bcurves.Select(s => new Bezier(s.points.Reverse().ToArray())).Reverse().ToList();


            // form the endcaps as lines
            var fs = fcurves[0].points[0];
            var fe = fcurves[len - 1].points[fcurves[len - 1].points.Length - 1];
            var bs = bcurves[len - 1].points[bcurves[len - 1].points.Length - 1];
            var be = bcurves[0].points[0];
            var ls = Utils.MakeLine(bs, fs);
            var le = Utils.MakeLine(fe, be);

            var segments = new List<Bezier> { ls };

            segments.AddRange(fcurves);
            segments.Add(le);
            segments.AddRange(bcurves);

            //slen = segments.length;

            return new PolyBezier(segments);
        }

        public PolyBezier Outline(float d1, float d2, float d3, float d4)
        {
            var reduced = this.Reduce();
            var len = reduced.Count;
            var fcurves = new List<Bezier>();
            var bcurves = new List<Bezier>();

            var alen = 0.0f;
            var tlen = this.Length;

            Func<float, float> linearDistanceFunction(float s, float e, float tlen0, float alen0, float slen0)
            {
                return new Func<float, float>(v =>
                 {
                     var f1 = alen0 / tlen0;
                     var f2 = (alen0 + slen0) / tlen0;
                     var d = e - s;
                     return Utils.Map(v, 0, 1, s + f1 * d, s + f2 * d);
                 });
            }

            // form curve oulines
            foreach (var segment in reduced)
            {
                var slen = segment.Length;

                fcurves.Add(segment.Scale(linearDistanceFunction(d1, d3, tlen, alen, slen)));
                bcurves.Add(segment.Scale(linearDistanceFunction(-d2, -d4, tlen, alen, slen)));

                alen += slen;
            }

            // reverse the "return" outline
            bcurves = bcurves.Select(s => new Bezier(s.points.Reverse().ToArray())).Reverse().ToList();


            // form the endcaps as lines
            var fs = fcurves[0].points[0];
            var fe = fcurves[len - 1].points[fcurves[len - 1].points.Length - 1];
            var bs = bcurves[len - 1].points[bcurves[len - 1].points.Length - 1];
            var be = bcurves[0].points[0];
            var ls = Utils.MakeLine(bs, fs);
            var le = Utils.MakeLine(fe, be);

            var segments = new List<Bezier> { ls };

            segments.AddRange(fcurves);
            segments.Add(le);
            segments.AddRange(bcurves);

            //slen = segments.length;

            return new PolyBezier(segments);
        }


        /// <summary>
        /// Reduces a curve to a collection of "simple" subcurves, where a simpleness is defined as having all control points on the same side of the baseline (cubics having the additional constraint that the control-to-end-point lines may not cross), and an angle between the end point normals no greater than 60 degrees.
        /// The main reason this function exists is to make it possible to scale curves.As mentioned in the offset function, curves cannot be offset without cheating, and the cheating is implemented in this function.The array of simple curves that this function yields can safely be scaled.
        /// </summary>
        public List<Bezier> Reduce(double step = 0.01)
        {
            List<Bezier> pass1 = new List<Bezier>();
            List<Bezier> pass2 = new List<Bezier>();

            // first pass: split on extrema
            List<float> extrema = Extrema().Values.ToList();
            for (var i = 0; i < extrema.Count; i++)
            {
                if (Utils.Approximately(extrema[i], 0))
                    extrema[i] = 0;
                else if (Utils.Approximately(extrema[i], 1))
                    extrema[i] = 1;
            }

            if (extrema.IndexOf(0) == -1)
            {
                extrema.Insert(0, 0);
            }
            if (extrema.IndexOf(1) == -1)
            {
                extrema.Add(1);
            }
            {
                float t1 = extrema[0];
                for (int i = 1; i < extrema.Count; i++)
                {
                    float t2 = extrema[i];
                    if (Utils.Approximately(t1, t2))
                        continue;

                    Bezier segment = Split(t1, t2);
                    segment._t1 = t1;
                    segment._t2 = t2;
                    pass1.Add(segment);
                    t1 = t2;
                }
            }

            int ii = 0;
            // second pass: further reduce these segments to simple segments
            foreach (Bezier p1 in pass1)
            {
                double t1 = 0f;
                double t2 = 0f;
                while (t2 <= 1)
                {
                    for (t2 = t1 + step; t2 <= 1 + step; t2 += step)
                    {
                        Bezier segment = p1.Split((float)t1, (float)t2);
                        if (!segment.IsSimple())
                        {
                            t2 -= step;
                            if (abs(t1 - t2) < step)
                            {
                                return pass2;
                            }
                            segment = p1.Split((float)t1, (float)t2);
                            segment._t1 = Utils.Map((float)t1, 0, 1, p1._t1, p1._t2);
                            segment._t2 = Utils.Map((float)t2, 0, 1, p1._t1, p1._t2);
                            pass2.Add(segment);
                            t1 = t2;
                            break;
                        }
                    }
                }
                if (t1 < 1)
                {
                    Bezier segment = p1.Split((float)t1, 1);
                    segment._t1 = Utils.Map((float)t1, 0, 1, p1._t1, p1._t2);
                    segment._t2 = p1._t2;
                    pass2.Add(segment);
                }

                ii++;
            }

            return pass2;
        }

        public Bezier Scale(Func<float, float> distanceFn)
        {
            var order = this.order;
            if (order == 2)
            {
                return this.Raise().Scale(distanceFn);
            }

            // TODO: add special handling for degenerate (=linear) curves.
            var clockwise = this.clockwise;
            var r1 = distanceFn(0);
            var r2 = distanceFn(1);

            var c0 = this.Position(0);
            var n0 = this.Normal(0);

            var c1 = this.Position(1);
            var n1 = this.Normal(1);

            var v0 = c0 + n0 * 10;
            var v1 = c1 + n1 * 10;

            var o = Utils.Lli4(v0, c0, v1, c1);

            if (o == null)
            {
                return null;
            }
            // move all points by distance 'd' wrt the origin 'o'
            var points = this.points;
            var np = new Vector2[order + 1];

            // move end points by fixed distance along normal.
            np[0] = points[0] + r1 * n0;
            np[order] = points[order] + r2 * n1;

            // move control points by "however much necessary to
            // ensure the correct tangent to endpoint".

            void function(int t)
            {
                if (this.order == 2 && (t != 0)) return;
                var p = points[t + 1];
                var ov = p - o.Value;

                var rc = distanceFn((t + 1.0f) / order);
                if (!clockwise)
                {
                    rc = -rc;
                }
                var m = sqrt(ov.X * ov.X + ov.Y * ov.Y);

                ov.X /= m;
                ov.Y /= m;

                np[t + 1] = p + rc * ov;
            }

            function(0);
            function(1);

            return new Bezier(np);
        }

        /// <summary>
        /// Scales a curve with respect to the intersection between the end point normals.
        /// Note that this will only work if that point exists, which is only guaranteed for simple segments.
        /// </summary>
        /// <param name="d">Distance</param>
        /// <returns>Scaled bezier if it's simple, null otherwise.</returns>
        public Bezier Scale(float d)
        {
            var order = this.order;

            // TODO: add special handling for degenerate (=linear) curves.
            var clockwise = this.clockwise;
            var r1 = d;
            var r2 = d;

            var c0 = this.Position(0);
            var n0 = this.Normal(0);

            var c1 = this.Position(1);
            var n1 = this.Normal(1);

            var v0 = c0 + n0 * 10;
            var v1 = c1 + n1 * 10;

            var o = Utils.Lli4(v0, c0, v1, c1);
            if (o == null)
            {
                return null;
            }

            // move all points by distance 'd' wrt the origin 'o'
            var points = this.points;
            var np = new Vector2[order + 1];

            // move end points by fixed distance along normal.
            np[0] = points[0] + r1 * n0;
            np[order] = points[order] + r2 * n1;


            // move control points to lie on the intersection of the offset
            // derivative vector, and the origin-through-control vector

            if (order != 2)
            {
                var p = np[0];
                var p2 = p + this.Tangent(0);
                np[1] = Utils.Lli4(p, p2, o.Value, points[1]).Value;
            }

            {
                var p = np[order];
                var p2 = p + this.Tangent(1);
                np[1 + 1] = Utils.Lli4(p, p2, o.Value, points[1 + 1]).Value;
            }
            return new Bezier(np);
        }


        /// <summary>
        /// Finds the intersections between this curve an some line. The intersections are an array of t values on this curve.
        /// 
        /// Curves are first aligned(translation/rotation) such that the curve's first coordinate is (0,0), and the curve is rotated so that the intersecting line coincides with the x-axis. Doing so turns "intersection finding" into plain "root finding".
        /// 
        /// As a root finding solution, the roots are computed symbolically for both quadratic and cubic curves, using the standard square root function which you might remember from high school, and the absolutely not standard Cardano's algorithm for solving the cubic root function.
        /// </summary>
        /// <param name="line">The line</param>
        /// <returns>An array of t values on this curve.</returns>
        public float[] Intersects(Line line)
        {
            var mx = Math.Min(line.P1.X, line.P2.X);
            var my = Math.Min(line.P1.Y, line.P2.Y);
            var MX = Math.Max(line.P1.X, line.P2.X);
            var MY = Math.Max(line.P1.Y, line.P2.Y);
            var self = this;

            return Utils.Roots(this.points, line).Where((t) =>
            {
                var p = self.Position(t);
                return Utils.Between(p.X, mx, MX) && Utils.Between(p.Y, my, MY);
            }).ToArray();
        }

        /// <summary>
        /// Finds the intersections between this curve and another.
        /// Intersections are yielded as a List of <see cref="Pair{T}" />, where the Left float corresponds to the t value on this curve, and the Right float corresponds to the t value on the other curve.
        ///
        /// Curve/curve intersection uses an interative process, where curves are subdivided at the midpoint, and bounding box overlap checks are performed between the resulting smaller curves. Any overlap is marked as a pair to resolve, and the "divide and check overlap" step is repeated. Doing this enough times "homes in" on the actual intersections, such that with infinite divisions, we can get an arbitrarily close approximation of the t values involved. Thankfully, repeating the process a low number of steps is generally good enough to get reliable values (typically 10 steps yields more than acceptable precision).
        /// </summary>
        /// <param name="curve">Other curve.</param>
        /// <param name="threshold">Threshold.</param>
        /// <returns>Intersection points.</returns>
        public List<Pair<float>> Intersects(Bezier curve, float threshold)
        {
            return Intersects(this.Reduce(), curve.Reduce(), threshold);
        }

        public List<Pair<float>> IntersectsWithSelf(float threshold)
        {
            var reduced = this.Reduce();
            // "simple" curves cannot intersect with their direct
            // neighbour, so for each segment X we check whether
            // it intersects [0:x-2][x+2:last].
            var len = reduced.Count - 2;
            var results = new List<Pair<float>>();

            for (var i = 0; i < len; i++)
            {
                var left = reduced.Skip(i).Take(1);
                var right = reduced.Skip(i + 2);
                var result = Intersects(left, right, threshold);
                results.AddRange(result);
            }

            return results;
        }

        private static List<Pair<float>> Intersects(IEnumerable<Bezier> c1, IEnumerable<Bezier> c2, float curveIntersectionThreshold)
        {
            var pairs = new List<Pair<Bezier>>();
            // step 1: pair off any overlapping segments
            foreach (var l in c1)
            {
                foreach (var r in c2)
                {
                    if (Overlaps(l, r))
                    {
                        pairs.Add(new Pair<Bezier> { Left = l, Right = r });
                    }
                }
            }

            // step 2: for each pairing, run through the convergence algorithm.
            var intersections = new List<Pair<float>>();

            foreach (var pair in pairs)
            {
                var result = Utils.PairIteration(
                  pair.Left,
                  pair.Right,
                  curveIntersectionThreshold
                );
                if (result.Count > 0)
                {
                    intersections.AddRange(result);
                }
            }

            return intersections;
        }

        public List<Arc> ToArcs(float errorThreshold)
        {
            var circles = new List<Arc>();
            this._iterate(errorThreshold, circles);


            foreach (var arc in circles)
            {
                arc.StartAngle = Angle.ToDegrees(arc.StartAngle);
                arc.EndAngle = Angle.ToDegrees(arc.EndAngle);
            }

            return circles;
        }

        float _error(Vector2 pc, Vector2 np1, float s, float e)
        {
            var q = (e - s) / 4;
            var c1 = this.Position(s + q);
            var c2 = this.Position(e - q);
            var @ref = Vector2.Distance(pc, np1);
            var d1 = Vector2.Distance(pc, c1);
            var d2 = Vector2.Distance(pc, c2);
            return abs(d1 - @ref) + abs(d2 - @ref);
        }

        List<Arc> _iterate(float errorThreshold, List<Arc> circles)
        {
            var t_s = 0.0f;
            var t_e = 1.0f;
            var safety = 0;
            // we do a binary search to find the "good `t` closest to no-longer-good"
            do
            {
                safety = 0;

                // step 1: start with the maximum possible arc
                t_e = 1;

                // points:
                var np1 = this.Position(t_s);
                Arc arc = null;
                Arc prev_arc = null;

                // booleans:
                var curr_good = false;
                var prev_good = false;
                bool done;

                // numbers:
                var t_m = t_e;
                var prev_e = 1.0f;
                var step = 0.0f;

                // step 2: find the best possible arc
                do
                {
                    prev_good = curr_good;
                    prev_arc = arc;
                    t_m = (t_s + t_e) / 2;
                    step++;

                    var np2 = this.Position(t_m);
                    var np3 = this.Position(t_e);

                    arc = Utils.Getccenter(np1, np2, np3);

                    var error = this._error(arc.Center, np1, t_s, t_e);
                    curr_good = error <= errorThreshold;

                    done = prev_good && !curr_good;
                    if (!done) prev_e = t_e;

                    // this arc is fine: we can move 'e' up to see if we can find a wider arc
                    if (curr_good)
                    {
                        // if e is already at max, then we're done for this arc.
                        if (t_e >= 1)
                        {
                            // make sure we cap at t=1
                            prev_e = 1;
                            prev_arc = arc;
                            // if we capped the arc segment to t=1 we also need to make sure that
                            // the arc's end angle is correct with respect to the bezier end point.
                            if (t_e > 1)
                            {
                                var d = new Vector2(arc.Center.X + arc.Radius * cos(arc.EndAngle), arc.Center.Y + arc.Radius * sin(arc.EndAngle));
                                arc.EndAngle += Utils.Angle(arc.Center, d, this.Position(1));
                            }
                            break;
                        }
                        // if not, move it up by half the iteration distance
                        t_e = t_e + (t_e - t_s) / 2;
                    }
                    else
                    {
                        // this is a bad arc: we need to move 'e' down to find a good arc
                        t_e = t_m;
                    }
                } while (!done && safety++ < 100);

                if (safety >= 100)
                {
                    break;
                }

                // console.log("L835: [F] arc found", t_s, prev_e, prev_arc.x, prev_arc.y, prev_arc.s, prev_arc.e);

                prev_arc = prev_arc ?? arc;
                circles.Add(prev_arc);
                t_s = prev_e;
            } while (t_e < 1);

            return circles;
        }


        /// <summary>
        /// Align this curve to a line defined by two points.
        /// </summary>
        /// <returns>Aligned Bezier.</returns>
        public Bezier Align(Vector2 start, Vector2 end)
        {
            var aligned = Utils.Align(points, new Line { P1 = start, P2 = end });

            return new Bezier(aligned.ToArray());
        }



        /// <summary>
        /// Get all 't' values for which this curve inflects.
        /// NOTE: this is an expensive operation!
        /// </summary>
        /// <returns>All 't' values for which this curve inflects</returns>
        public float[] Inflections()
        {
            return Utils.Inflections(points);
        }

        //float[] getInflections()
        //{
        //    float[] ret = { };
        //    var t_values = new List<float>();
        //    t_values.Add(0.0f);
        //    t_values.Add(1.0f);
        //    float[] roots;
        //    // get first derivative roots
        //    roots = comp.findAllRoots(1, x_values);
        //    foreach (float t in roots) { if (0 < t && t < 1) { t_values.Add(t); } }
        //    roots = comp.findAllRoots(1, y_values);
        //    foreach (float t in roots) { if (0 < t && t < 1) { t_values.Add(t); } }
        //    // get second derivative roots
        //    if (order > 2)
        //    {
        //        roots = comp.findAllRoots(2, x_values);
        //        foreach (float t in roots) { if (0 < t && t < 1) { t_values.Add(t); } }
        //        roots = comp.findAllRoots(2, y_values);
        //        foreach (float t in roots) { if (0 < t && t < 1) { t_values.Add(t); } }
        //    }
        //    // sort roots
        //    ret = new float[t_values.Count];
        //    for (int i = 0; i < ret.Length; i++) { ret[i] = t_values[i]; }
        //    ret = sort(ret);
        //    // remove duplicates
        //    t_values = new List<float>();
        //    foreach (float f in ret) { if (!t_values.Contains(f)) { t_values.Add(f); } }
        //    ret = new float[t_values.Count];
        //    for (int i = 0; i < ret.Length; i++) { ret[i] = t_values[i]; }
        //    if (ret.Length > (2 * order + 2))
        //    {
        //        //var errMsg = "ERROR: getInflections is returning way too many roots (" + ret.Length + ")";
        //        return new float[0];
        //    }
        //    return ret;
        //}

        private static void CheckLinear(Bezier curve)
        {
            int order = curve.order;
            Vector2[] points = curve.points;
            Vector2[] a = Utils.Align(points, new Line { P1 = points[0], P2 = points[order] }).ToArray();

            for (int i = 0; i < a.Length; i++)
            {
                if (abs(a[i].Y) > 0.0001)
                {
                    curve._linear = false;
                    return;
                }
            }
            curve._linear = true;
        }

        /// <summary>
        /// Converts the curve into SVG path string.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            Vector2[] p = points;
            float x = p[0].X;
            float y = p[0].Y;
            List<object> s = new List<object> { "M", x, y, order == 2 ? "Q" : "C" };

            for (int i = 1; i < p.Length; i++)
            {
                s.Add(p[i].X);
                s.Add(p[i].Y);
            }

            return string.Join(" ", s);
        }














        private static float sqrt(float v)
        {
            return (float)Math.Sqrt(v);
        }
        private static float abs(float v)
        {
            return (float)Math.Abs(v);
        }
        private static float abs(double v)
        {
            return (float)Math.Abs(v);
        }

        private static float pow(float a, float b)
        {
            return (float)Math.Pow(a, b);
        }

        // cube root function yielding real roots
        private static float crt(float v)
        {
            return v < 0 ? -pow(-v, 1 / 3) : pow(v, 1 / 3);
        }

        // trig constants
        private const float pi = 3.1415926535897931f;
        private const float tau = 2 * pi;
        private const float quart = pi / 2;

        private static float acos(float v)
        {
            return (float)Math.Acos(v);
        }
        private static float cos(float v)
        {
            return (float)Math.Cos(v);
        }

        private static float sin(float v)
        {
            return (float)Math.Sin(v);
        }

        private static float atan2(float y, float x)
        {
            return (float)Math.Atan2(y, x);
        }

        public void Move(Vector2 offset)
        {
            for (var i = 0; i < points.Length; i++)
            {
                points[i] += offset;
            }

            ComputeDerivativeCoordinates();
        }

        Pair<float>[] IPathShape.Intersects(Circle circle)
        {
            throw new NotImplementedException();
        }

        Pair<float>[] IPathShape.Intersects(Arc arc)
        {
            throw new NotImplementedException();
        }

        Pair<float>[] IPathShape.Intersects(Line line)
        {
            throw new NotImplementedException();
        }

        private float[] ExtremaX()
        {
            var extrema = this.Extrema().Values.ToList();

            if (extrema.Count == 0) return new float[] { 0, 1 };

            //ensure leading zero
            if (Utils.Approximately(extrema[0], 0))
                extrema[0] = 0;
            else
                extrema.Insert(0, 0);

            //ensure ending 1
            if (Utils.Approximately(extrema[extrema.Count - 1], 1))
                extrema[extrema.Count - 1] = 1;
            else
                extrema.Add(1);

            return extrema.ToArray();
        }

        public IEnumerable<IPathShape> ToArcs()
        {
            var extrema = this.ExtremaX();
            var accuracy = Length / 100.0f;

            for (int i = 1; i < extrema.Length; i++)
            {
                if (Utils.Approximately(extrema[i - 1], extrema[i]))
                    continue;

                var arcs = GetArcs(accuracy * (extrema[i] - extrema[i - 1]), extrema[i - 1], extrema[i]);

                foreach (var arc in arcs)
                {
                    yield return arc;
                }
            }
        }

        private IEnumerable<IPathShape> GetArcs(float accuracy, float startT, float endT)
        {
            while (startT < endT)
            {
                var arc = GetLargestArc(this, startT, endT, accuracy);
                if (arc == null)
                    yield break;

                var iarc = arc as IInterval;

                if (Utils.Approximately(startT, iarc.Interval.End.Value))
                    yield return arc;

                startT = iarc.Interval.End.Value;

                float len = arc.Length;
                if (len < 0.0001)
                    continue;

                yield return arc;
            }
        }

        private static IPathShape GetLargestArc(Bezier b, float startT, float endT, float accuracy)
        {
            Arc lastGoodArc = null;
            TPoint start = new TPoint(b, startT);
            TPoint end = new TPoint(b, endT);
            TPoint upper = end;
            TPoint lower = start;
            int count = 0;
            TPoint test = upper;
            bool? reversed = null;

            while (count < 100)
            {
                Vector2 middle = b.Position((start.t + test.t) / 2);
                var arc = Arc.FromPoints(start.point, middle, test.point);
                IInterval iarc = arc;

                //if the 3 points are linear, this may throw
                if (arc == null)
                {
                    if (lastGoodArc != null)
                    {
                        return lastGoodArc;
                    }
                    else
                    {
                        break;
                    }
                }


                //only need to test once to see if this arc is polar / clockwise
                if (reversed == null)
                {
                    reversed = start.point == Point.FromAngleOnCircle(arc.EndAngle, arc);
                }

                //now we have a valid arc, measure the error.
                float error = GetError(b, startT, test.t, arc, reversed.Value);

                //if error is within accuracy, this becomes the lower
                if (error <= accuracy)
                {
                    iarc.Interval = new Interval
                    {
                        Start = startT,
                        End = test.t
                    };
                    lower = test;
                    lastGoodArc = arc;
                }
                else
                {
                    upper = test;
                }

                //exit if lower is the end
                if (Utils.Approximately(lower.t, upper.t) || (lastGoodArc != null && (lastGoodArc != arc) && (Angle.OfArcSpan(arc) - Angle.OfArcSpan(lastGoodArc)) < 0.5))
                {
                    return lastGoodArc;
                }

                count++;
                test = new TPoint(b, (lower.t + upper.t) / 2);
            }

            //arc failed, so return a line
            var line = new Line(start.point, test.point);
            IInterval iline = line;

            iline.Interval = new Interval
            {
                Start = startT,
                End = test.t
            };

            return line;
        }

        private static float GetError(Bezier b, float startT, float endT, IPathShape arc, bool arcReversed)
        {
            float tSpan = endT - startT;

            float m(float ratio)
            {
                float t = startT + tSpan * ratio;
                Vector2 bp = b.Position(t);
                Vector2 ap = arc.Position(arcReversed ? 1 - ratio : ratio);
                return Vector2.Distance(ap, bp);
            }

            return m(0.25f) + m(0.75f);
        }

        private class TPoint
        {
            public Vector2 point;
            public float t;

            public TPoint(Bezier b, float t, Vector2 offset = default)
            {
                this.t = t;
                point = b.Position(t) + offset;
            }
        }

        //internal override int NumberOfKeyPoints(float maxPointDistance = 0)
        //{
        //    throw new System.NotImplementedException();
        //}

        //public override List<Vector2> ToKeyPoints(float maxArcFacet = 0)
        //{
        //    var curve = new BezierCurve(this);
        //    List<Vector2> curveKeyPoints = new List<Vector2>();

        //    curve.FindChains((chains, loose, layer, ignored) =>
        //    {
        //        if (chains.Length == 1)
        //        {
        //            var c = chains[0];
        //            switch (c.Links[0].WalkedPath.PathId)
        //            {
        //                case "Arc_0":
        //                case "Line_0":
        //                    break;
        //                default:
        //                    c.Reverse();
        //                    break;
        //            }
        //            curveKeyPoints = new List<Vector2>(c.ToKeyPoints());
        //        }
        //        else if (loose.Length == 1)
        //        {
        //            curveKeyPoints = loose[0].PathContext.ToKeyPoints();
        //        }
        //    });

        //    return curveKeyPoints;
        //}
    }
}