using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace AnimationExtensions
{
    public delegate double EasingEquation(double t, double b, double c, double d);

    public delegate void ActionHandler<T>(T element);

    public class Ax
    {
        public static Animation New()
        {
            return new Animation();
        }

        public static Animation New(FrameworkElement element)
        {
            return new Animation(element);
        }

        public static Animation New(string animationName)
        {
            return new Animation(animationName);
        }

        public static Animation Wait(double duration = 0)
        {
            return new Animation().Wait(duration);
        }

        public static Prototype Pro()
        {
            return new Prototype();
        }

        public static Animation For<T>(IEnumerable<T> list, Func<int, T, Prototype> predicate)
        {
            return new Animation().For(list, predicate);
        }

        public static Prototype Move(double x = 0, double y = 0, double duration = 0, EasingEquation eq = null)
        {
            return Ax.Pro().Move(x, y, duration, eq);
        }

        public static Prototype Fade(double opacity = 0, double duration = 0, EasingEquation eq = null)
        {
            return Ax.Pro().Fade(opacity, duration, eq);
        }
    }

    public class Factory
    {
        public static event EventHandler Start;
        public static event EventHandler Change;
        public static event EventHandler Stop;

        static List<Animation> animations = new List<Animation>();
        public static int instances = 0;

        public static void Write(int indent, string input)
        {
            for (int i = 0; i < indent; i++)
                input = "   " + input;
            Debug.WriteLine(input);
        }

        public static void Add(Animation animation)
        {
            if (animation == null)
                return;
            instances++;


            if (instances == 1)
                RaiseStart();
            else
                RaiseChange();

            //if (animations.Contains(animation))
            //    throw new Exception("Add exception");

            //animations.Add(animation);
        }

        public static void Remove(Animation animation)
        {
            if (animation == null)
                return;

            instances--;

            if (instances == 0)
            {
                //GC.WaitForPendingFinalizers();
                //GC.Collect();

                RaiseStop();
            }
            else
            {
                RaiseChange();
            }
        }

        private static void RaiseStop()
        {
            if (Stop != null)
                Stop(null, null);
        }

        private static void RaiseChange()
        {
            if (Change != null)
                Change(null, null);
        }

        private static void RaiseStart()
        {
            if (Start != null)
                Start(null, null);
        }
    }

    public class Animation
    {
        List<Group> groups = new List<Group>();
        int currentGroup;

        FrameworkElement element;
        bool isfinished = false;
        bool isattached = false;

        static int instances = 0;
        public static int GetInstances()
        {
            return instances;
        }

        ~Animation()
        {
            instances--;
        }

        public Animation(FrameworkElement element)
            : this()
        {
            this.element = element;
        }

        public Animation(string groupName = null)
        {
            this.groups.Add(new Group(groupName));
            instances++;

        }

        #region Chain Extensions

        public Animation For<T>(IEnumerable<T> list, Func<int, T, Prototype> predicate)
        {
            int i = 0;
            foreach (T item in list)
            {
                var x = predicate(i++, item);
                this.And(x);
            }

            return this;
        }

        public Animation ForThen<T>(IEnumerable<T> list, Func<int, T, Prototype> predicate)
        {
            int i = 0;
            foreach (T item in list)
            {
                var x = predicate(i++, item);
                this.AndThen(x);
            }

            return this;
        }

        public Animation ForBack<T>(IEnumerable<T> list, Func<int, T, Prototype> predicate)
        {
            int i = list.Count() - 1;
            foreach (T item in list)
            {
                var x = predicate(i--, item);
                this.And(x);
            }

            return this;
        }

        public Animation And(Animation animation)
        {
            var group = this.CurrentGroup();
            group.AndAnimation(animation);
            return this;
        }

        public Animation And(Prototype prototype)
        {
            var group = this.CurrentGroup();
            group.Prototype(prototype, this.element ?? prototype.element);
            return this;
        }

        public Animation And(Prototype prototype, FrameworkElement element)
        {
            return this.And(prototype.Copy(element));
        }

        public Animation AndWait(Animation animation, double duration)
        {
            var ax = Ax.New().Wait(duration).And(animation);
            return this.And(ax);
        }

        public Animation AndWait(Prototype prototype, double duration)
        {
            var ax = Ax.New().Wait(duration).And(prototype);
            return this.And(ax);
        }

        public Animation And(Animation animation, string groupName)
        {
            if (!string.IsNullOrEmpty(groupName))
            {
                var newGroup = animation.groups.FirstOrDefault(g => g.Name == groupName);
                if (newGroup != null)
                {
                    var groupIndex = animation.groups.IndexOf(newGroup);
                    var group = this.CurrentGroup();
                    group.AndAnimation(animation, groupIndex);
                }
                else
                {
                    throw new Exception(string.Format(@"Group name {0} already exists",
                        groupName));
                }
            }

            return this;
        }

        public Animation AndThen(Animation animation)
        {
            return this.And(animation).Then();
        }

        public Animation AndThen(Prototype prototype)
        {
            return this.And(prototype).Then();
        }

        public Animation AndThen(Prototype prototype, FrameworkElement element)
        {
            return this.And(prototype, element).Then();
        }

        public Animation Do(ActionHandler<FrameworkElement> handler)
        {
            var group = this.CurrentGroup();
            group.Do(handler, this.element);
            return this;
        }

        public Animation Then()
        {
            this.NextGroup();
            return this;
        }

        public Animation ThenDo(ActionHandler<FrameworkElement> handler)
        {
            return this.Then().Do(handler);
        }

        public Animation ThenStop()
        {
            var group = this.NextGroup();
            group.Stop(e => this.End(), this.element);
            return this;
        }

        public Animation ThenPause(string groupName = null)
        {
            var group = this.NextGroup();
            group.Do(e => this.DetachEvents(), this.element);
            this.NextGroup(groupName);
            return this;
        }

        public Animation DoPlay(Animation animation)
        {
            this.Do(e => animation.Begin());
            return this;
        }

        public Animation DoStop(Animation animation)
        {
            this.Do(e => animation.End());
            return this;
        }

        public Animation ThenPause()
        {
            var group = this.NextGroup();
            group.Do(e => this.DetachEvents(), this.element);
            this.NextGroup();
            return this;
        }

        public Animation ThenResume()
        {
            var group = this.NextGroup();
            group.Do(e => this.AttachEvents(), this.element);
            return this;
        }

        public Animation Wait(double duration)
        {
            var group = this.NextGroup();
            group.Wait(duration);
            this.NextGroup();
            return this;
        }

        #endregion

        private Group CurrentGroup()
        {
            return this.groups[this.currentGroup];
        }

        private Group NextGroup(string groupName = null)
        {
            if (this.FindIndexForGroupName(groupName) >= 0)
                throw new Exception(string.Format(@"Group {0} already exists.", groupName));

            this.groups.Add(new Group(groupName));
            this.currentGroup++;

            return this.CurrentGroup();
        }

        internal Animation Begin(int group = 0, bool repeat = false)
        {
            if (!repeat)
            {
                var lastGroup = this.groups.LastOrDefault();
                var lastFrame = lastGroup.frames.LastOrDefault();

                if (!(lastFrame is StopFrame))
                    this.ThenStop();
            }

            this.Reset();
            this.currentGroup = group;

            this.AttachEvents();
            return this;
        }

        internal Animation Begin(string groupName, bool repeat = false)
        {
            return this.Begin(this.FindIndexForGroupName(groupName), repeat);
        }

        //internal Task<Animation> BeginAsync(int group = 0, bool repeat = false)
        //{
        //    if (!repeat)
        //    {
        //        var lastGroup = this.groups.LastOrDefault();
        //        var lastFrame = lastGroup.frames.LastOrDefault();

        //        if (!(lastFrame is StopFrame))
        //            this.ThenStop();
        //    }

        //    this.Reset();
        //    this.currentGroup = group;

        //    this.AttachEvents();

        //    var task = new Task<Animation>(this.Run);
        //    task.Start();

        //    return task;
        //}

        internal Animation Run()
        {
            while (!this.isfinished);
            return this;
        }

        public Animation Play()
        {
            return this.Begin();
        }

        public Task<Animation> PlayAsync()
        {
            this.Play();

            var task = new Task<Animation>(() => {
                while (!this.isfinished) ;
                return this;
            });

            task.Start();
            return task;
        }

        public Animation Resume()
        {
            this.AttachEvents();
            return this;
        }

        public Animation Repeat()
        {
            return this.Begin(repeat: true);
        }

        public Animation Play(string groupName, bool repeat = false)
        {
            return this.Begin(groupName, repeat);
        }

        public void Stop()
        {
            this.End();
        }

        internal int FindIndexForGroupName(string groupName)
        {
            if (!string.IsNullOrEmpty(groupName))
            {
                var group = this.groups.FirstOrDefault(g => g.Name == groupName);
                if (group != null)
                {
                    var groupIndex = this.groups.IndexOf(group);
                    return groupIndex;
                }
            }

            return -1;
        }

        internal bool IsRunning()
        {
            return this.isattached;
        }

        internal void Reset()
        {
            foreach (var group in groups)
                group.Reset();

            this.isfinished = false;
        }

        internal void Update()
        {
            var group = this.groups[this.currentGroup];
            group.Update();

            if (group.Finished())
                this.currentGroup++;

            if (currentGroup == this.groups.Count)
            {
                this.Reset();
                currentGroup = 0;
            }
        }

        internal void End()
        {
            this.currentGroup = 0;
            this.isfinished = true;
            this.DetachEvents();
        }

        private void AttachEvents()
        {
            if (this.isfinished)
                this.Begin();

            else if (!this.isattached)
            {
                CompositionTarget.Rendering += CompositionTarget_Rendering;
                Factory.Add(this);
                this.isattached = true;
            }
        }

        private void DetachEvents()
        {
            if (this.isattached)
            {
                CompositionTarget.Rendering -= CompositionTarget_Rendering;
                Factory.Remove(this);
                this.isattached = false;
            }
        }

        private void CompositionTarget_Rendering(object sender, EventArgs e)
        {
            this.Update();
        }

        public void Output(int indent = 0)
        {
            var elementName = default(string);
            if (this.element != null)
                elementName = this.element.ToString();

            Factory.Write(indent, "Animation: " +
                "- Finished: " + this.isfinished.ToString() +
                "- Attached: " + this.isattached.ToString() +
                "- Groups: " + this.groups.Count.ToString());

            foreach (var group in groups)
                group.Output(indent + 1);
        }
    }

    public class Group
    {
        internal List<Frame> frames = new List<Frame>();

        public string Name { get; set; }

        public Group(string groupName = null)
        {
            this.Name = groupName;
        }

        internal void Prototype(Prototype prototype, FrameworkElement element)
        {
            frames.Add(new PrototypeFrame(prototype, element));
        }

        internal void Do(ActionHandler<FrameworkElement> action, FrameworkElement element)
        {
            frames.Add(new DoFrame(action, element));
        }

        internal void Stop(ActionHandler<FrameworkElement> action, FrameworkElement element)
        {
            frames.Add(new StopFrame(action, element));
        }

        internal void Wait(double duration)
        {
            frames.Add(new WaitFrame(duration));
        }

        internal void Update()
        {
            foreach (var frame in frames)
            {
                if (!frame.Finished())
                    frame.Update();
            }
        }

        internal bool Finished()
        {
            return frames.All(b => b.Finished());
        }

        internal void AndAnimation(Animation animation, int groupIndex = 0)
        {
            frames.Add(new AnimationChildFrame(animation, true, groupIndex));
        }

        internal void Reset()
        {
            foreach (var frame in frames)
            {
                frame.Reset();
            }
        }

        internal void Output(int indent)
        {
            foreach (var frame in frames)
                frame.Output(indent);
        }
    }

    public class Prototype
    {
        public FrameworkElement element;
        private List<Effect> effects = new List<Effect>();

        public Prototype()
        {

        }

        public Prototype(FrameworkElement element)
        {
            this.element = element;
        }

        public Prototype(FrameworkElement element, List<Effect> list)
            : this(element)
        {
            foreach (var effect in list)
            {
                this.effects.Add(effect.Copy());
            }
        }

        public Prototype And(Prototype prototype)
        {
            foreach (var effect in prototype.effects)
                this.effects.Add(effect.Copy());

            return this;
        }

        public Animation Repeat()
        {
            return this.New().Repeat();
        }

        public Animation Play()
        {
            return this.New().Play();
        }

        public Task<Animation> PlayAsync()
        {
            return this.New().PlayAsync();
        }

        internal Task PlayAsync(FrameworkElement element)
        {
            return this.New(element).PlayAsync();
        }

        public Animation New()
        {
            return Ax.New(this.element).And(this);
        }

        public Animation New(FrameworkElement element)
        {
            return Ax.New(element).And(this);
        }

        public Prototype Move(out MoveEffect moveEffect, double x = 0, double y = 0, double duration = 0, EasingEquation eq = null)
        {
            moveEffect = new MoveEffect(x, y, duration, eq);
            this.effects.Add(moveEffect);
            return this;
        }

        public Prototype Move(double x = 0, double y = 0, double duration = 0, EasingEquation eq = null)
        {
            this.effects.Add(new MoveEffect(x, y, duration, eq));
            return this;
        }

        public Prototype MoveTo(FrameworkElement item, FrameworkElement itemTo, Point distance = new Point(), double duration = 0, EasingEquation eq = null)
        {
            if (distance == new Point())
                distance = new Point(1, 1);

            item.UpdateLayout();
            itemTo.UpdateLayout();

            Rect b = item.GetBoundsRelativeTo(itemTo) ?? new Rect();
            var d = Math.Sqrt(b.Top * b.Top + b.Left * b.Left);
            var a = Math.Atan2(-b.Top, -b.Left);
            var x = Math.Cos(a) * d * distance.X;
            var y = Math.Sin(a) * d * distance.Y;

            this.effects.Add(new MoveEffect(x, y, duration, eq));
            return this;
        }

        public Prototype MoveTo(FrameworkElement itemTo, Point distance = new Point(), double duration = 0, EasingEquation eq = null)
        {
            return this.MoveTo(this.element, itemTo, distance, duration, eq);
        }

        public Prototype Fade(double opacity = 0, double duration = 0, EasingEquation eq = null)
        {
            this.effects.Add(new FadeEffect(opacity, duration, eq));
            return this;
        }

        public Prototype Rotate(double angle = 0, double duration = 0, EasingEquation eq = null)
        {
            this.effects.Add(new RotateEffect(angle, duration, eq));
            return this;
        }

#if SILVERLIGHT
        public Prototype Plane(double x = 0, double y = 0, double z = 0, double duration = 0, EasingEquation eq = null)
        {
            this.effects.Add(new PlaneEffect(x, y, z, duration, eq));
            return this;
        }
#endif

        public Prototype Scale(double x = 1, double y = 1, double duration = 0, EasingEquation eq = null)
        {
            this.effects.Add(new ScaleEffect(x, y, duration, eq));
            return this;
        }

        public Prototype Size(double x = -1, double y = -1, double duration = 0, EasingEquation eq = null)
        {
            this.effects.Add(new SizeEffect(x, y, duration, eq));
            return this;
        }

        public Prototype Size(Point size, double duration = 0, EasingEquation eq = null)
        {
            this.effects.Add(new SizeEffect(size.X, size.Y, duration, eq));
            return this;
        }


        public Prototype Then()
        {
            this.effects.Add(new ThenEffect());
            return this;
        }

        public Prototype Do(ActionHandler<FrameworkElement> handler)
        {
            this.effects.Add(new DoEffect<FrameworkElement>(handler));
            return this;
        }

        public Prototype Do<T>(ActionHandler<T> actionHandler)
            where T : FrameworkElement
        {
            this.effects.Add(new DoEffect<T>(actionHandler));
            return this;
        }

        public Prototype ThenDo(ActionHandler<FrameworkElement> handler)
        {
            return this.Then().Do(handler);
        }

        public Prototype ThenDo<T>(ActionHandler<T> actionHandler)
            where T : FrameworkElement
        {
            this.Then();
            this.effects.Add(new DoEffect<T>(actionHandler));
            return this;
        }

        public Prototype Wait(double duration = 0)
        {
            this.effects.Add(new WaitEffect(duration));
            return this.Then();
        }

        public Animation ThenStop()
        {
            return Ax.New().And(this).ThenStop();
        }

        internal void Reset()
        {
            foreach (var effect in this.effects)
            {
                effect.Reset();
            }
        }

        internal void Update(FrameworkElement element)
        {
            bool allFinishedUntilNow = true;

            element = this.element ?? element;

            foreach (var effect in this.effects)
            {
                if (!effect.isfinished)
                {
                    if (effect is ThenEffect)
                    {
                        if (!allFinishedUntilNow)
                            break;
                    }

                    effect.Update(element);

                    allFinishedUntilNow &= effect.isfinished;
                }
            }
        }

        internal bool Finished()
        {
            return this.effects.All(e => e.isfinished);
        }

        internal void Output(int indent)
        {
            foreach (var effect in effects)
            {
                effect.Output(indent);
            }
        }

        internal Prototype Copy(FrameworkElement element)
        {
            return new Prototype(element, this.effects);
        }

#if (SILVERLIGHT && !WINDOWS_PHONE)
        public Prototype Blur(double targetBlurValue = 0, double duration = 0, EasingEquation eq = null)
        {
            this.effects.Add(new BlurEffect(targetBlurValue, duration, eq));
            return this;
        }
#endif

        
    }

    public class Effect
    {
        internal struct Manipulation
        {
            public double start;
            public double offset;
            public double target;
        }

        protected EasingEquation easingEquation;
        protected DateTime startTime;
        protected double duration;

        public bool isrunning = false;
        public bool isfinished = false;

        public Effect(double duration = 0, EasingEquation eq = null)
        {
            this.easingEquation = eq ?? Eq.Linear;

            this.duration = duration;
            this.isrunning = false;
        }

        internal double Manipulate(Manipulation m, double elapsedMilliseconds, EasingEquation eq)
        {
            return m.start + eq(elapsedMilliseconds, 0, m.offset, this.duration);
        }

        internal virtual void Init(FrameworkElement element)
        {
        }

        internal void Update(FrameworkElement element)
        {
            // If not running, Init
            if (!this.isrunning)
            {
                this.startTime = DateTime.Now;
                this.isfinished = false;
                this.isrunning = true;

                if (duration > 0)
                    this.Init(element);
            }

            var t = (DateTime.Now - startTime).TotalMilliseconds;
            bool finished = (duration == 0 || t >= duration);
            if (finished)
            {
                this.isfinished = true;
                this.isrunning = false;
                this.Finished(element);
            }
            else
            {
                this.Update(element, t);
            }
        }

        internal virtual void Update(FrameworkElement element, double elapsedMilliseconds)
        {

        }

        internal virtual void Finished(FrameworkElement element)
        {
        }

        internal void Reset()
        {
            this.isfinished = false;
            this.isrunning = false;
        }

        internal virtual void Output(int indent)
        {
            Factory.Write(indent + 1, this.GetType().ToString().Replace(this.GetType().Namespace, string.Empty)
                + " - IsFinished: " + this.isfinished.ToString()
                + " - IsRunning: " + this.isrunning.ToString());
        }

        internal virtual Effect Copy()
        {
            return new Effect(this.duration, this.easingEquation);
        }
    }

    public class MoveEffect : Effect
    {
        EasingEquation easingEquationY;

        Manipulation x;
        Manipulation y;

        public MoveEffect(double x = 0, double y = 0, double duration = 0, EasingEquation eq = null, EasingEquation ey = null)
            : base(duration, eq)
        {
            this.x.target = x;
            this.y.target = y;

            this.easingEquationY = ey;
        }

        internal override void Init(FrameworkElement element)
        {
            var translateTransform = element.GetTransformation<TranslateTransform>();
            if (translateTransform != null)
            {
                // Set current position 
                x.start = translateTransform.X;
                y.start = translateTransform.Y;
            }
            else
            {
                x.start = 0;
                y.start = 0;
            }

            x.offset = x.target - x.start;
            y.offset = y.target - y.start;
        }

        internal override void Update(FrameworkElement element, double elapsedMilliseconds)
        {
            // Try to get seperate easing function for Y
            EasingEquation easingEquationY = this.easingEquationY ?? this.easingEquation;

            element.SetMove(
                Manipulate(x, elapsedMilliseconds, this.easingEquation),
                Manipulate(y, elapsedMilliseconds, easingEquationY));
        }

        internal override void Finished(FrameworkElement element)
        {
            element.SetMove(x.target, y.target);
        }

        internal override Effect Copy()
        {
            var effect = new MoveEffect(this.x.target, this.y.target,
                this.duration, this.easingEquation, this.easingEquationY);

            return effect;
        }
    }

    public class FadeEffect : Effect
    {
        Manipulation opacity;

        /// <summary>
        /// Initializes the Effect
        /// </summary>
        /// <param name="targetOpacity"></param>
        /// <param name="duration"></param>
        public FadeEffect(double targetOpacity, double duration, EasingEquation easingFunction = null)
            : base(duration, easingFunction)
        {
            this.opacity.target = targetOpacity;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="element"></param>
        internal override void Init(FrameworkElement element)
        {

            opacity.start = element.Opacity;
            opacity.offset = opacity.target - opacity.start;


            //Storyboard sb = new Storyboard();

            //DoubleAnimation animation = new DoubleAnimation();
            //animation.Duration = new Duration(TimeSpan.FromMilliseconds(duration)); ;
            //animation.From = opacity.start;
            //animation.To = opacity.target;

            //sb.Children.Add(animation);
            //Storyboard.SetTarget(animation, element);
            //Storyboard.SetTargetProperty(animation, new PropertyPath("Opacity"));

            //sb.Begin();
        }

        /// <summary>
        /// Renders the effect
        /// </summary>
        /// <param name="element"></param>
        internal override void Update(FrameworkElement element, double elapsedMilliseconds)
        {
            element.Opacity = this.Manipulate(opacity, elapsedMilliseconds, this.easingEquation);
        }

        internal override void Finished(FrameworkElement element)
        {
            element.Opacity = opacity.target;
            base.Finished(element);
        }

        internal override Effect Copy()
        {
            var effect = new FadeEffect(this.opacity.target,
                this.duration, this.easingEquation);

            return effect;
        }
    }

#if (SILVERLIGHT && !WINDOWS_PHONE)
    public class BlurEffect : Effect
    {
        Manipulation blurRadius;

        /// <summary>
        /// Initializes the Effect
        /// </summary>
        /// <param name="targetBlurValue"></param>
        /// <param name="duration"></param>
        public BlurEffect(double targetBlurValue, double duration, EasingEquation easingFunction = null)
            : base(duration, easingFunction)
        {
            this.blurRadius.target = targetBlurValue;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="element"></param>
        internal override void Init(FrameworkElement element)
        {
            var controlEffect = (element.Effect as System.Windows.Media.Effects.BlurEffect)
               ?? new System.Windows.Media.Effects.BlurEffect();

            blurRadius.start = controlEffect.Radius;
            blurRadius.offset = blurRadius.target - blurRadius.start;
        }

        /// <summary>
        /// Renders the effect
        /// </summary>
        /// <param name="controlElement"></param>
        internal override void Update(FrameworkElement element, double elapsedMilliseconds)
        {
            element.SetBlur(this.Manipulate(blurRadius, elapsedMilliseconds, this.easingEquation));
        }

        internal override void Finished(FrameworkElement element)
        {
            element.SetBlur(blurRadius.target);
            base.Finished(element);
        }

        internal override Effect Copy()
        {
            var effect = new BlurEffect(this.blurRadius.target,
                this.duration, this.easingEquation);

            return effect;
        }
    }
#endif

    public class RotateEffect : Effect
    {
        Manipulation angle;

        /// <summary>
        /// Initializes the Effect
        /// </summary>
        /// <param name="angle"></param>
        /// <param name="duration"></param>
        public RotateEffect(double angle, double duration, EasingEquation easingFunction = null)
            : base(duration, easingFunction)
        {
            this.angle.target = angle;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="element"></param>
        internal override void Init(FrameworkElement element)
        {
            // element.RenderTransformOrigin = new Point(0.5f, 0.5f);

            var rotateTransform = element.GetTransformation<RotateTransform>();

            angle.start = rotateTransform != null
                ? rotateTransform.Angle
                : 0;

            angle.offset = angle.target - angle.start;
        }

        /// <summary>
        /// Renders the effect
        /// </summary>
        /// <param name="controlElement"></param>
        internal override void Update(FrameworkElement element, double elapsedMilliseconds)
        {
            element.SetRotation(this.Manipulate(angle, elapsedMilliseconds, this.easingEquation));
        }

        internal override void Finished(FrameworkElement element)
        {
            element.SetRotation(angle.target);
        }

        internal override Effect Copy()
        {
            var effect = new RotateEffect(this.angle.target,
                this.duration, this.easingEquation);

            return effect;
        }
    }

#if (SILVERLIGHT)
    public class PlaneEffect : Effect
    {
        Manipulation x;
        Manipulation y;
        Manipulation z;

        /// <summary>
        /// Initializes the Effect
        /// </summary>
        /// <param name="angle"></param>
        /// <param name="duration"></param>
        public PlaneEffect(double x, double y, double z, double duration, EasingEquation easingFunction = null)
            : base(duration, easingFunction)
        {
            this.x.target = x;
            this.y.target = y;
            this.z.target = z;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="element"></param>
        internal override void Init(FrameworkElement element)
        {
            var planeProjection = (element.Projection as PlaneProjection) ?? new PlaneProjection();
            
            this.x.start = planeProjection.RotationX;
            this.y.start = planeProjection.RotationY;
            this.z.start = planeProjection.RotationZ;
            this.x.offset = this.x.target - this.x.start;
            this.y.offset = this.y.target - this.y.start;
            this.z.offset = this.z.target - this.z.start;
        }

        /// <summary>
        /// Renders the effect
        /// </summary>
        /// <param name="controlElement"></param>
        internal override void Update(FrameworkElement element, double elapsedMilliseconds)
        {
            element.SetPlane(
               this.Manipulate(x, elapsedMilliseconds, this.easingEquation),
               this.Manipulate(y, elapsedMilliseconds, this.easingEquation),
               this.Manipulate(z, elapsedMilliseconds, this.easingEquation));
        }

        internal override void Finished(FrameworkElement element)
        {
            element.SetPlane(x.target, y.target, z.target);
        }

        internal override Effect Copy()
        {
            var effect = new PlaneEffect(x.target, y.target, z.target,
                this.duration, this.easingEquation);

            return effect;
        }
    }
#endif

    public class ScaleEffect : Effect
    {
        Manipulation x;
        Manipulation y;

        /// <summary>
        /// Initializes the Effect
        /// </summary>
        /// <param name="angle"></param>
        /// <param name="duration"></param>
        public ScaleEffect(double x, double y, double duration, EasingEquation easingFunction = null)
            : base(duration, easingFunction)
        {
            this.x.target = x;
            this.y.target = y;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="element"></param>
        internal override void Init(FrameworkElement element)
        {
            var scaleTransform = element.GetTransformation<ScaleTransform>();

            //element.RenderTransformOrigin = new Point(0.5f, 0.5f);

            if (scaleTransform != null)
            {
                // Set current position 
                x.start = scaleTransform.ScaleX;
                y.start = scaleTransform.ScaleY;
            }
            else
            {
                x.start = 1;
                y.start = 1;
            }

            // Set current position 
            x.offset = x.target - x.start;
            y.offset = y.target - y.start;
        }

        /// <summary>
        /// Renders the effect
        /// </summary>
        /// <param name="controlElement"></param>
        internal override void Update(FrameworkElement element, double elapsedMilliseconds)
        {
            element.SetScale(
               this.Manipulate(x, elapsedMilliseconds, this.easingEquation),
               this.Manipulate(y, elapsedMilliseconds, this.easingEquation));
        }

        internal override void Finished(FrameworkElement element)
        {
            element.SetScale(x.target, y.target);
        }

        internal override Effect Copy()
        {
            var effect = new ScaleEffect(x.target, y.target,
                this.duration, this.easingEquation);

            return effect;
        }
    }

    public class SizeEffect : Effect
    {
        Manipulation x;
        Manipulation y;

        /// <summary>
        /// Initializes the Effect
        /// </summary>
        /// <param name="angle"></param>
        /// <param name="duration"></param>
        public SizeEffect(double x, double y, double duration, EasingEquation easingFunction = null)
            : base(duration, easingFunction)
        {
            this.x.target = x;
            this.y.target = y;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="element"></param>
        internal override void Init(FrameworkElement element)
        {
            element.InvalidateArrange();

            if (x.target >= 0)
            {
                x.start = element.RenderSize.Width;
                x.offset = x.target - x.start;
            }

            if (y.target >= 0)
            {
                y.start = element.RenderSize.Height;
                y.offset = y.target - y.start;
            }
        }

        /// <summary>
        /// Renders the effect
        /// </summary>
        /// <param name="controlElement"></param>
        internal override void Update(FrameworkElement element, double elapsedMilliseconds)
        {
            double nx = x.target;
            double ny = y.target;

            if (x.target >= 0) nx = this.Manipulate(x, elapsedMilliseconds, this.easingEquation);
            if (y.target >= 0) ny = this.Manipulate(y, elapsedMilliseconds, this.easingEquation);

            element.SetSize(nx, ny);
        }

        internal override void Finished(FrameworkElement element)
        {
            element.SetSize(x.target, y.target);
        }

        internal override Effect Copy()
        {
            var effect = new SizeEffect(x.target, y.target,
                this.duration, this.easingEquation);

            return effect;
        }
    }

    public class ThenEffect : Effect
    {
        public ThenEffect(double duration = 0)
            : base(duration, null)
        {

        }

        internal override Effect Copy()
        {
            return new ThenEffect();
        }
    }

    public class WaitEffect : ThenEffect
    {
        public WaitEffect(double duration = 0)
            : base(duration)
        {

        }

        internal override Effect Copy()
        {
            return new WaitEffect(this.duration);
        }
    }

    public class DoEffect<T> : Effect
        where T : FrameworkElement
    {
        ActionHandler<T> handler;

        public DoEffect(ActionHandler<T> handler)
            : base(0, null)
        {
            this.handler = handler;
        }

        internal override void Finished(FrameworkElement element)
        {
            handler(element as T);
            base.Finished(element);
        }
        internal override Effect Copy()
        {
            return new DoEffect<T>(this.handler);
        }
    }

    public class Frame
    {
        internal virtual void Update()
        {

        }

        internal virtual bool Finished()
        {
            return true;
        }

        internal virtual void Reset()
        {

        }

        internal virtual void Output(int indent)
        {
            Factory.Write(indent + 1, this.GetType().ToString().Replace(this.GetType().Namespace, string.Empty) + " - " + this.Finished().ToString());
        }
    }

    public class PrototypeFrame : Frame
    {
        private Prototype prototype;
        private FrameworkElement element;

        public PrototypeFrame(Prototype prototype, FrameworkElement element)
        {
            this.prototype = prototype;
            this.element = element;
        }

        internal override void Update()
        {
            this.prototype.Update(this.element);
            base.Update();
        }

        internal override bool Finished()
        {
            return this.prototype.Finished();
        }

        internal override void Reset()
        {
            this.prototype.Reset();
            base.Reset();
        }

        internal override void Output(int indent)
        {
            base.Output(indent);
            prototype.Output(indent + 1);
        }
    }

    public class DoFrame : Frame
    {
        private ActionHandler<FrameworkElement> action;
        private FrameworkElement element;
        bool isFinished = false;

        public DoFrame(ActionHandler<FrameworkElement> action, FrameworkElement element)
        {
            this.action = action;
            this.element = element;
        }

        internal override void Reset()
        {
            this.isFinished = false;
        }

        internal override bool Finished()
        {
            return this.isFinished;
        }

        internal override void Update()
        {
            this.action(this.element);
            this.isFinished = true;
            base.Update();
        }
    }

    public class StopFrame : DoFrame
    {
        public StopFrame(ActionHandler<FrameworkElement> action, FrameworkElement element)
            : base(action, element)
        {
        }
    }

    public class WaitFrame : Frame
    {
        private DateTime startTime;
        private bool iswaiting;
        private double duration;

        public WaitFrame(double duration)
        {
            this.duration = duration;
        }

        internal override void Update()
        {
            if (!this.iswaiting)
            {
                this.startTime = DateTime.Now;
                this.iswaiting = true;
            }

            base.Update();
        }

        internal override void Reset()
        {
            this.iswaiting = false;
            base.Reset();
        }

        internal override bool Finished()
        {
            var t = (DateTime.Now - startTime).TotalMilliseconds;
            return iswaiting && (duration == 0 || t >= duration);
        }
    }

    public class AnimationChildFrame : Frame
    {
        private Animation animation;

        private bool isstarted = false;
        private bool isfinished = false;
        bool startOrStop;

        private int groupIndex;

        public AnimationChildFrame(Animation animation, bool startOrStop = true)
        {
            this.animation = animation;
            this.startOrStop = startOrStop;
        }

        public AnimationChildFrame(Animation animation, bool startOrStop = true, int groupIndex = 0)
        {
            // TODO: Complete member initialization
            this.animation = animation;
            this.startOrStop = startOrStop;
            this.groupIndex = groupIndex;
        }

        internal override void Update()
        {
            if (!this.isstarted)
            {
                if (this.startOrStop)
                    this.animation.Begin(groupIndex);
                else
                    this.animation.End();

                this.isstarted = true;
            }

            base.Update();
        }

        internal override void Reset()
        {
            this.isstarted = false;
            base.Reset();
        }

        internal override bool Finished()
        {
            isfinished = this.isstarted && !this.animation.IsRunning();
            return isfinished;
        }

        internal override void Output(int indent)
        {
            base.Output(indent);
            animation.Output(indent + 2);
        }
    }

    public class Eq
    {
        public static EasingEquation OutBounce = (t, b, c, d) =>
        {
            if ((t /= d) < (1 / 2.75)) return c * (7.5625 * t * t) + b;
            else if (t < (2 / 2.75)) return c * (7.5625 * (t -= (1.5 / 2.75)) * t + .75) + b;
            else if (t < (2.5 / 2.75)) return c * (7.5625 * (t -= (2.25 / 2.75)) * t + .9375) + b;
            else return c * (7.5625 * (t -= (2.625 / 2.75)) * t + .984375) + b;
        };

        public static EasingEquation InBounce = (t, b, c, d) =>
        {
            if ((t /= d) < (1 / 2.75)) return c * (7.5625 * t * t) + b;
            else if (t < (2 / 2.75)) return c * (7.5625 * (t -= (1.5 / 2.75)) * t + .75) + b;
            else if (t < (2.5 / 2.75)) return c * (7.5625 * (t -= (2.25 / 2.75)) * t + .9375) + b;
            else return c * (7.5625 * (t -= (2.625 / 2.75)) * t + .984375) + b;
            //return c - InBounce(d - t, 0, c, d) + b;
        };

        public static EasingEquation InOutBounce = (t, b, c, d) =>
        {
            if (t < d / 2) return InBounce(t * 2, 0, c, d) * .5 + b;
            else return InOutBounce(t * 2 - d, 0, c, d) * .5 + c * .5 + b;
        };

        public static EasingEquation Linear = (t, b, c, d) =>
        {
            return c * t / d + b;
        };

        public static EasingEquation InQuart = (t, b, c, d) =>
        {
            return c * (t /= d) * t * t * t + b;
        };

        public static EasingEquation InOutQuart = (t, b, c, d) =>
        {
            if ((t /= d / 2) < 1) return c / 2 * t * t * t * t + b;
            return -c / 2 * ((t -= 2) * t * t * t - 2) + b;
        };

        public static EasingEquation OutQuart = (t, b, c, d) =>
        {
            return -c * ((t = t / d - 1) * t * t * t - 1) + b;
        };

        public static EasingEquation OutBack = (t, b, c, d) =>
        {
            return c * ((t = t / d - 1) * t * ((1.70158 + 1) * t + 1.70158) + 1) + b;
        };

        public static EasingEquation InBack = (t, b, c, d) =>
        {
            return c * (t /= d) * t * ((1.70158 + 1) * t - 1.70158) + b;
        };

        public static EasingEquation InOutBack = (t, b, c, d) =>
        {
            double s = 1.70158;
            if ((t /= d / 2) < 1) return c / 2 * (t * t * (((s *= (1.525)) + 1) * t - s)) + b;
            return c / 2 * ((t -= 2) * t * (((s *= (1.525)) + 1) * t + s) + 2) + b;
        };

        public static EasingEquation OutElastic = (t, b, c, d) =>
        {
            if ((t /= d) == 1) return b + c;
            double p = d * .3;
            double s = p / 4;
            return (c * Math.Pow(2, -10 * t) * Math.Sin((t * d - s) * (2 * Math.PI) / p) + c + b);
        };

        public static EasingEquation InElastic = (t, b, c, d) =>
        {
            if ((t /= d) == 1) return b + c;
            double p = d * .3;
            double s = p / 4;
            return -(c * Math.Pow(2, 10 * (t -= 1)) * Math.Sin((t * d - s) * (2 * Math.PI) / p)) + b;
        };

        public static EasingEquation InOutElastic = (t, b, c, d) =>
        {
            if ((t /= d / 2) == 2) return b + c;
            double p = d * (.3 * 1.5);
            double s = p / 4;
            if (t < 1) return -.5 * (c * Math.Pow(2, 10 * (t -= 1)) * Math.Sin((t * d - s) * (2 * Math.PI) / p)) + b;
            return c * Math.Pow(2, -10 * (t -= 1)) * Math.Sin((t * d - s) * (2 * Math.PI) / p) * .5 + c + b;
        };

        public static EasingEquation OutCubic = (t, b, c, d) =>
        {
            return c * ((t = t / d - 1) * t * t + 1) + b;
        };

        public static EasingEquation InCubic = (t, b, c, d) =>
        {
            return c * (t /= d) * t * t + b;
        };

        public static EasingEquation InOutCubic = (t, b, c, d) =>
        {
            if ((t /= d / 2) < 1) return c / 2 * t * t * t + b;
            return c / 2 * ((t -= 2) * t * t + 2) + b;
        };

        public static EasingEquation OutExpo = (t, b, c, d) =>
        {
            return (t == d) ? b + c : c * (-Math.Pow(2, -10 * t / d) + 1) + b;
        };

        public static EasingEquation InExpo = (t, b, c, d) =>
        {
            return (t == 0) ? b : c * Math.Pow(2, 10 * (t / d - 1)) + b;
        };

        public static EasingEquation InOutExpo = (t, b, c, d) =>
        {
            if (t == 0) return b;
            if (t == d) return b + c;
            if ((t /= d / 2) < 1) return c / 2 * Math.Pow(2, 10 * (t - 1)) + b;
            return c / 2 * (-Math.Pow(2, -10 * --t) + 2) + b;
        };

        public static EasingEquation InQuad = (t, b, c, d) =>
        {
            return -c * (t /= d) * (t - 2) + b;
        };

        public static EasingEquation OutQuad = (t, b, c, d) =>
        {
            return c * (t /= d) * t + b;
        };

        public static EasingEquation InOutQuad = (t, b, c, d) =>
        {
            if ((t /= d / 2) < 1) return c / 2 * t * t + b;
            return -c / 2 * ((--t) * (t - 2) - 1) + b;
        };

        public static EasingEquation OutSine = (t, b, c, d) =>
        {
            return c * Math.Sin(t / d * (Math.PI / 2)) + b;
        };

        public static EasingEquation InSine = (t, b, c, d) =>
        {
            return -c * Math.Cos(t / d * (Math.PI / 2)) + c + b;
        };

        public static EasingEquation InOutSine = (t, b, c, d) =>
        {
            if ((t /= d / 2) < 1) return c / 2 * (Math.Sin(Math.PI * t / 2)) + b;
            return -c / 2 * (Math.Cos(Math.PI * --t / 2) - 2) + b;
        };

        public static EasingEquation OutCirc = (t, b, c, d) =>
        {
            return c * Math.Sqrt(1 - (t = t / d - 1) * t) + b;
        };

        public static EasingEquation InCirc = (t, b, c, d) =>
        {
            return -c * (Math.Sqrt(1 - (t /= d) * t) - 1) + b;
        };

        public static EasingEquation InOutCirc = (t, b, c, d) =>
        {
            if ((t /= d / 2) < 1) return -c / 2 * (Math.Sqrt(1 - t * t) - 1) + b;
            return c / 2 * (Math.Sqrt(1 - (t -= 2) * t) + 1) + b;
        };


        public static EasingEquation OutQuint = (t, b, c, d) =>
        {
            return c * ((t = t / d - 1) * t * t * t * t + 1) + b;
        };

        public static EasingEquation InQuint = (t, b, c, d) =>
        {
            return c * (t /= d) * t * t * t * t + b;
        };

        public static EasingEquation InOutQuint = (t, b, c, d) =>
        {
            if ((t /= d / 2) < 1) return c / 2 * t * t * t * t * t + b;
            return c / 2 * ((t -= 2) * t * t * t * t + 2) + b;
        };


    }

    public static class Extensions
    {
        public static void Shuffle<T>(this IList<T> list)
        {
            Random rng = new Random();
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }

        public static Animation For<T>(this IEnumerable<T> list, Func<int, T, Prototype> predicate)
            where T : FrameworkElement
        {
            return Ax.New().For(list, predicate);
        }

        public static void For<T>(this IEnumerable<T> items, Action<T, int> predicate)
            where T : FrameworkElement
        {
            int i = 0;
            foreach (T item in items)
            {
                predicate(item, i++);
            }
        }

        public static T GetContainerForIndex<T>(this ItemsControl itemsControl, int itemIndex)
           where T : FrameworkElement
        {
            var contentPresenter = itemsControl.ItemContainerGenerator.ContainerFromIndex(itemIndex);
            return VisualTreeHelper.GetChild(contentPresenter, 0) as T;
        }

        public static FrameworkElement GetContainerForIndex(this ItemsControl itemsControl, int itemIndex)
        {
            // Get the ItemsPanel
            var contentPresenter = itemsControl.ItemContainerGenerator.ContainerFromIndex(itemIndex) as ContentPresenter;
            return contentPresenter;
        }


        public static Point GetSize(this FrameworkElement element)
        {
            return new Point(element.ActualWidth, element.ActualHeight);
        }


        /// <summary>
        /// Get the bounds of an element relative to another element.
        /// </summary>
        /// <param name="element">The element.</param>
        /// <param name="otherElement">
        /// The element relative to the other element.
        /// </param>
        /// <returns>
        /// The bounds of the element relative to another element, or null if
        /// the elements are not related.
        /// </returns>
        /// <exception cref="T:System.ArgumentNullException">
        /// <paramref name="element"/> is null.
        /// </exception>
        /// <exception cref="T:System.ArgumentNullException">
        /// <paramref name="otherElement"/> is null.
        /// </exception>
        public static Rect? GetBoundsRelativeTo(this FrameworkElement element, UIElement otherElement, bool forceUpdates = false)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }
            else if (otherElement == null)
            {
                throw new ArgumentNullException("otherElement");
            }

            try
            {
                if (forceUpdates)
                {
                    element.UpdateLayout();
                    otherElement.UpdateLayout();
                }

                Point origin, bottom;
                GeneralTransform transform = element.TransformToVisual(otherElement);
                if (transform != null &&
                    transform.TryTransform(new Point(), out origin) &&
                    transform.TryTransform(new Point(element.ActualWidth, element.ActualHeight), out bottom))
                {
                    return new Rect(origin, bottom);
                }
            }
            catch (ArgumentException)
            {
                // Ignore any exceptions thrown while trying to transform
            }

            return null;
        }
    }

    public static class TransformExtensions
    {
        public static void RenderTransformOrigins<T>(this IEnumerable<T> list, Point point)
            where T : FrameworkElement
        {
            foreach (var item in list)
                item.RenderTransformOrigin = point;
        }
        /// <summary>
        /// Extension method that returns the Transformation of type T for a UIElement
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="uiElement"></param>
        /// <returns></returns>
        public static void AddTransform<T>(this UIElement uiElement, T transform)
            where T : Transform
        {
            // Find the rendertransformgroup
            var renderTransform = uiElement.RenderTransform;
            if (renderTransform is TransformGroup)
            {
                TransformGroup transformGroup = (TransformGroup)renderTransform;
                bool found = false;

                // Loop through all the children and find the transformation type
                for (int i = 0; i < transformGroup.Children.Count; i++)
                {
                    if (transformGroup.Children[i] is T)
                    {
                        transformGroup.Children.RemoveAt(i);
                        transformGroup.Children.Add(transform);
                        found = true;
                        break;
                    }
                }

                if (!found)
                    transformGroup.Children.Add(transform);

                uiElement.RenderTransform = transformGroup;
            }
            else if (renderTransform != null)
            {
                var transformGroup = new TransformGroup();

                if (!(renderTransform is T))
                    transformGroup.Children.Add(renderTransform);

                transformGroup.Children.Add(transform);
                uiElement.RenderTransform = transformGroup;
            }
            else
            {
                uiElement.RenderTransform = transform;
            }
        }
        /// <summary>
        /// Extension method that returns the Transformation of type T for a UIElement
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="uiElement"></param>
        /// <returns></returns>
        public static T GetTransformation<T>(this UIElement uiElement)
        {
            // Create a default return value
            T translate = default(T);

            // Find the rendertransformgroup
            var renderTransform = uiElement.RenderTransform;
            if (renderTransform is TransformGroup)
            {
                TransformGroup transformGroup = (TransformGroup)renderTransform;

                // Loop through all the children and find the transformation type
                foreach (var transform in transformGroup.Children)
                {
                    if (transform is T)
                        translate = (T)(object)transform;
                }
            }

            return translate;
        }
    }

    public static class FrameworkElementExtensions
    {

        public static DependencyObject GetRootVisual(this DependencyObject element)
        {
            if (element == null)
                return null;

            var returnValue = default(DependencyObject);
            var parent = VisualTreeHelper.GetParent(element) as DependencyObject;

            while (parent != null)
            {
                returnValue = parent;
                parent = VisualTreeHelper.GetParent(returnValue) as DependencyObject;
            }

            return returnValue;
        }


        public static bool IsVisibleChild(this FrameworkElement container, FrameworkElement element)
        {
            var root = container.GetRootVisual() as UIElement;

            var scrollTransform = container.TransformToVisual(root);
            var elementTransform = element.TransformToVisual(root);

            Rect scrollRectangle = new Rect(scrollTransform.Transform(new Point()), container.RenderSize);
            Rect elementRectangle = new Rect(elementTransform.Transform(new Point()), element.RenderSize);

            if (scrollRectangle.Left <= elementRectangle.Right && scrollRectangle.Right >= elementRectangle.Left &&
                             scrollRectangle.Top <= elementRectangle.Bottom && scrollRectangle.Bottom >= elementRectangle.Top)
            {
                return true;
            }

            return false;
        }

        public static Animation New(this FrameworkElement element)
        {
            return element.Pro().New();
        }

        public static void Hide<T>(this IEnumerable<T> items)
            where T : FrameworkElement
        {
            foreach (var item in items)
                item.Hide();
        }

        public static void UpdateLayouts<T>(this IEnumerable<T> items)
            where T : FrameworkElement
        {
            foreach (var item in items)
                item.UpdateLayout();
        }

        public static void UpdateLayoutsAndHide<T>(this IEnumerable<T> items)
            where T : FrameworkElement
        {
            items.UpdateLayouts();
            items.Hide();
        }

        public static void Hide(this FrameworkElement element)
        {
            element.Opacity = 0;
        }


        public static void Show(this FrameworkElement element)
        {
            element.Opacity = 1;
        }


        public static void SetMove(this FrameworkElement element, double x = 0, double y = 0)
        {
            element.AddTransform<TranslateTransform>(new TranslateTransform()
            {
                X = x,
                Y = y
            });
        }

#if (SILVERLIGHT && !WINDOWS_PHONE)
        public static void SetBlur(this FrameworkElement element, double blurValue = 0)
        {
            var controlEffect = (element.Effect as System.Windows.Media.Effects.BlurEffect)
                           ?? new System.Windows.Media.Effects.BlurEffect();

            controlEffect.Radius = blurValue;
            element.Effect = controlEffect;
        }
#endif

#if SILVERLIGHT
        public static void SetPlane(this FrameworkElement element, double x = 0, double y = 0, double z = 0)
        {
            var planeProjection = (element.Projection as PlaneProjection) ?? new PlaneProjection();
            planeProjection.RotationX = x;
            planeProjection.RotationY = y;
            planeProjection.RotationZ = z;
            planeProjection.CenterOfRotationX = 0;
            element.Projection = planeProjection;
        }
#endif

        public static void SetRotation(this FrameworkElement element, double angle = 0)
        {
            var rotateTransform = new RotateTransform() { Angle = angle };
            element.AddTransform<RotateTransform>(rotateTransform);
        }

        public static void SetSize(this FrameworkElement element, double x = 0, double y = 0)
        {
            if (x > 0) element.Width = x >= 0 ? x : 0;
            if (y > 0) element.Height = y >= 0 ? y : 0;
        }

        public static void SetScale(this FrameworkElement element, double x = 0, double y = 0)
        {
            // Apply transforms
            var scaleTransform = new ScaleTransform()
            {
                ScaleX = x,
                ScaleY = y
            };

            element.AddTransform<ScaleTransform>(scaleTransform);
        }



        public static Prototype Move(this FrameworkElement element, double x = 0, double y = 0, double duration = 0, EasingEquation eq = null)
        {
            return element.Pro().Move(x, y, duration, eq);
        }

        public static Prototype MoveTo(this FrameworkElement element, FrameworkElement itemTo, Point distance = new Point(), double duration = 0, EasingEquation eq = null)
        {
            return element.Pro().MoveTo(itemTo, distance, duration, eq);
        }

        public static Prototype Fade(this FrameworkElement element, double opacity = 0, double duration = 0, EasingEquation eq = null)
        {
            return element.Pro().Fade(opacity, duration, eq);
        }

#if (SILVERLIGHT && !WINDOWS_PHONE)
        public static Prototype Blur(this FrameworkElement element, double targetBlurValue = 0, double duration = 0, EasingEquation eq = null)
        {
            return element.Pro().Blur(targetBlurValue, duration, eq);
        }
#endif

#if SILVERLIGHT
        public static Prototype Plane(this FrameworkElement element, double x = 0, double y = 0, double z = 0, double duration = 0, EasingEquation eq = null)
        {
            return element.Pro().Plane(x, y, z, duration, eq);
        }
#endif

        public static Prototype Rotate(this FrameworkElement element, double angle = 0, double duration = 0, EasingEquation eq = null)
        {
            return element.Pro().Rotate(angle, duration, eq);
        }

        public static Prototype Scale(this FrameworkElement element, double x = 0, double y = 0, double duration = 0, EasingEquation eq = null)
        {
            return element.Pro().Scale(x, y, duration, eq);
        }

        public static Prototype Size(this FrameworkElement element, double x = -1, double y = -1, double duration = 0, EasingEquation eq = null)
        {
            return element.Pro().Size(x, y, duration, eq);
        }

        public static Prototype Wait(this FrameworkElement element, double duration = 0)
        {
            return element.Pro().Wait(duration);
        }

        internal static Prototype Pro(this FrameworkElement element)
        {
            return new Prototype(element);
        }

        public static Prototype Pro(this FrameworkElement element, Prototype prototype)
        {
            return prototype.Copy(element);
        }

        public static Animation Begin(this FrameworkElement element, Prototype prototype)
        {
            var animation = element.Pro(prototype).New();
            animation.Begin();
            return animation;
        }
    }

    public class ItemsControl : System.Windows.Controls.ItemsControl
    {
        public double ItemOpacity
        {
            get { return (double)GetValue(ItemOpacityProperty); }
            set { SetValue(ItemOpacityProperty, value); }
        }

        public static readonly DependencyProperty ItemOpacityProperty =
            DependencyProperty.Register("ItemOpacity", typeof(double), typeof(ItemsControl), new PropertyMetadata(1d));

        public class ItemsGeneratedEventArgs : EventArgs
        {
            public bool AllItemsGenerated { get; set; }
            public int LastItemIndex { get; set; }

            public ItemsGeneratedEventArgs(bool allItemsGenerated, int lastItemIndex)
            {
                this.AllItemsGenerated = allItemsGenerated;
                this.LastItemIndex = lastItemIndex;
            }
        }

        public event EventHandler<ItemsGeneratedEventArgs> ItemsGenerated;

        bool isResetEvent = false;
        int containerCount = 0;

        public T GetContainerForIndex<T>(int itemIndex)
            where T : FrameworkElement
        {
            var contentPresenter = this.ItemContainerGenerator.ContainerFromIndex(itemIndex);
            return VisualTreeHelper.GetChild(contentPresenter, 0) as T;
        }

        public FrameworkElement GetContainerForIndex(int itemIndex)
        {
            var contentPresenter = this.ItemContainerGenerator.ContainerFromIndex(itemIndex) as ContentPresenter;
            return contentPresenter;
        }

        protected override void OnItemsChanged(System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Reset:
                    isResetEvent = true;
                    containerCount = 0;
                    break;

                case NotifyCollectionChangedAction.Remove:
                    containerCount -= e.OldItems.Count;
                    break;
                default:
                    break;
            }

            Debug.WriteLine(e.Action);

            base.OnItemsChanged(e);
        }

        protected override DependencyObject GetContainerForItemOverride()
        {
            var baseContainer = base.GetContainerForItemOverride();

            // Start by checking for ItemsGenerated listeners
            if (this.ItemsGenerated != null)
            {
                // Determine if the generated container is last in the Items list
                bool isLastItem = containerCount == this.Items.Count - 1;
                if (isLastItem)
                {
                    // Store these so the dispatcher can pick them up
                    bool wasResetEvent = isResetEvent;
                    int lastItemIndex = containerCount;

                    // Raise ItemsGenerator on the UI
                    //Dispatcher.BeginInvoke(() =>
                    //{
                    this.ItemsGenerated(this, new ItemsGeneratedEventArgs(wasResetEvent, lastItemIndex));
                    //});

                    isResetEvent = false;
                }
            }

            var childElement = baseContainer as FrameworkElement;
            if (childElement != null)
                childElement.Opacity = this.ItemOpacity;

            containerCount++;

            return baseContainer;
        }
    }
}
