using System;
using System.Collections.Generic;
using UnityEngine;

namespace UniRx {

    // ReSharper disable once PartialTypeWithSinglePart
    public static partial class ObservableTween {

        public enum EaseType {
            Linear,
            InQuadratic,
            OutQuadratic,
            InOutQuadratic,
            InCubic,
            OutCubic,
            InOutCubic,
            InQuartic,
            OutQuartic,
            InOutQuartic,
            InQuintic,
            OutQuintic,
            InOutQuintic,
            InSinusoidal,
            OutSinusoidal,
            InOutSinusoidal,
            InExponential,
            OutExponential,
            InOutExponential,
            InCircular,
            OutCircular,
            InOutCircular,
            InBack,
            OutBack,
            InOutBack,
            InBounce,
            OutBounce,
            InOutBounce,
            InElastic,
            OutElastic,
            InOutElastic,
        }

        public enum LoopType {
            /// <summary>
            /// ループなし
            /// </summary>
            None,
            /// <summary>
            /// 同じ Easing を繰り返す
            /// </summary>
            Repeat,
            /// <summary>
            /// 同じ Easing を start/finish を入れ替えて繰り返す
            /// </summary>
            PingPong,
            /// <summary>
            /// 行きの Easing に対応する帰りの Easing を繰り返す
            /// </summary>
            Mirror,
        }

        private static readonly Dictionary<EaseType, EaseType> MIRROR_EASE_TYPE_MAP = new Dictionary<EaseType, EaseType>() {
            { EaseType.Linear, EaseType.Linear },
            { EaseType.InQuadratic, EaseType.OutQuadratic },
            { EaseType.OutQuadratic, EaseType.InQuadratic },
            { EaseType.InOutQuadratic, EaseType.InOutQuadratic },
            { EaseType.InCubic, EaseType.OutCubic },
            { EaseType.OutCubic, EaseType.InCubic },
            { EaseType.InOutCubic, EaseType.InOutCubic },
            { EaseType.InQuartic, EaseType.OutQuartic },
            { EaseType.OutQuartic, EaseType.InQuartic },
            { EaseType.InOutQuartic, EaseType.InOutQuartic },
            { EaseType.InQuintic, EaseType.OutQuintic },
            { EaseType.OutQuintic, EaseType.InQuintic },
            { EaseType.InOutQuintic, EaseType.InOutQuintic },
            { EaseType.InSinusoidal, EaseType.OutSinusoidal },
            { EaseType.OutSinusoidal, EaseType.InSinusoidal },
            { EaseType.InOutSinusoidal, EaseType.InOutSinusoidal },
            { EaseType.InExponential, EaseType.OutExponential },
            { EaseType.OutExponential, EaseType.InExponential },
            { EaseType.InOutExponential, EaseType.InOutExponential },
            { EaseType.InCircular, EaseType.OutCircular },
            { EaseType.OutCircular, EaseType.InCircular },
            { EaseType.InOutCircular, EaseType.InOutCircular },
            { EaseType.InBack, EaseType.OutBack },
            { EaseType.OutBack, EaseType.InBack },
            { EaseType.InOutBack, EaseType.InOutBack },
            { EaseType.InBounce, EaseType.OutBounce },
            { EaseType.OutBounce, EaseType.InBounce },
            { EaseType.InOutBounce, EaseType.InOutBounce },
            { EaseType.InElastic, EaseType.OutElastic },
            { EaseType.OutElastic, EaseType.InElastic },
            { EaseType.InOutElastic, EaseType.InOutElastic },
        };

        private static readonly Dictionary<Type, Type> OPERATABLE_STRUCT_MAP = new Dictionary<Type, Type>() {
            { typeof(int), typeof(OperatableInt) },
            { typeof(float), typeof(OperatableFloat) },
            { typeof(Vector2), typeof(OperatableVector2) },
            { typeof(Vector3), typeof(OperatableVector3) },
        };

        public static IObservable<T> Tween<T>(T start, T finish, float duration, EaseType easeType, LoopType loopType = LoopType.None) where T : struct {
            return Tween(() => start, () => finish, () => duration, easeType, loopType);
        }

        public static IObservable<T> Tween<T>(T start, T finish, Func<float> duration, EaseType easeType, LoopType loopType = LoopType.None) where T : struct {
            return Tween(() => start, () => finish, duration, easeType, loopType);
        }

        public static IObservable<T> Tween<T>(Func<T> start, Func<T> finish, float duration, EaseType easeType, LoopType loopType = LoopType.None) where T : struct {
            return Tween(start, finish, () => duration, easeType, loopType);
        }

        public static IObservable<T> Tween<T>(Func<T> start, Func<T> finish, Func<float> duration, EaseType easeType, LoopType loopType = LoopType.None) where T : struct {
            return Tween(
                () => Activator.CreateInstance(OPERATABLE_STRUCT_MAP[typeof(T)], start()) as OperatableBase<T>,
                () => Activator.CreateInstance(OPERATABLE_STRUCT_MAP[typeof(T)], finish()) as OperatableBase<T>,
                duration,
                easeType,
                loopType
            );
        }

        private struct TweenInformation<T> where T : struct {

            public float Time { get; set; }

            public float StartTime { get; }

            public OperatableBase<T> Start { get; }

            public OperatableBase<T> Finish { get; }

            public float Duration { get; }

            public EaseType EaseType { get; }

            public TweenInformation(float startTime, OperatableBase<T> start, OperatableBase<T> finish, float duration, EaseType easeType, out T startValue, out T finishValue) {
                this.Time = startTime;
                this.StartTime = startTime;
                this.Start = start;
                this.Finish = finish;
                this.Duration = duration;
                this.EaseType = easeType;
                startValue = start.Value;
                finishValue = finish.Value;
            }

        }

        private static IObservable<T> Tween<T>(Func<OperatableBase<T>> start, Func<OperatableBase<T>> finish, Func<float> duration, EaseType easeType, LoopType loopType) where T : struct {
            T startValue = default(T);
            T finishValue = default(T);
            Func<IObserver<T>, IDisposable> returnStartValue = (observer) => {
                observer.OnNext(startValue);
                return null;
            };
            Func<IObserver<T>, IDisposable> returnFinishValue = (observer) => {
                observer.OnNext(finishValue);
                return null;
            };
            IObservable<T> stream = Observable.Empty<TweenInformation<T>>()
                // Repeat() のために、毎回初期値を生成
                .StartWith(() => new TweenInformation<T>(Time.time, start(), finish(), duration(), easeType, out startValue, out finishValue))
                // Update のストリームに変換
                .SelectMany(information => Observable.EveryUpdate().Do(_ => information.Time = Time.time - information.StartTime).Select(_ => information))
                // Tween 時間が処理時間よりも小さい間流し続ける
                .TakeWhile(information => information.Time <= information.Duration)
                // 実際の Easing 処理実行
                .Select(information => Easing(information.Time, information.Start, (information.Finish - information.Start), information.Duration, information.EaseType).Value)
                // 最終フレームの値を確実に流すために OnCompleted が来たら値を一つ流すストリームに繋ぐ
                .Concat(Observable.Create(returnFinishValue).Take(1));
            switch (loopType) {
                case LoopType.None:
                    // Do nothing.
                    break;
                case LoopType.Repeat:
                    stream = stream.Repeat();
                    break;
                case LoopType.PingPong:
                    stream = stream
                        .Concat(
                            Observable.Empty<TweenInformation<T>>()
                                // Repeat() のために、毎回初期値を生成
                                .StartWith(() => new TweenInformation<T>(Time.time, start(), finish(), duration(), easeType, out startValue, out finishValue))
                                // Update のストリームに変換
                                .SelectMany(information => Observable.EveryUpdate().Do(_ => information.Time = Time.time - information.StartTime).Select(_ => information))
                                // Tween 時間が処理時間よりも小さい間流し続ける
                                .TakeWhile(information => information.Time <= information.Duration)
                                // start と finish を入れ替えて、実際の Easing 処理実行
                                .Select(information => Easing(information.Time, information.Finish, (information.Start - information.Finish), information.Duration, information.EaseType).Value)
                                // 最終フレームの値を確実に流すために OnCompleted が来たら最終値を一つ流すストリームに繋ぐ
                                .Concat(Observable.Create(returnStartValue).Take(1))
                        )
                        .Repeat();
                    break;
                case LoopType.Mirror:
                    stream = stream
                        .Concat(
                            Observable.Empty<TweenInformation<T>>()
                                // Repeat() のために、毎回初期値を生成
                                .StartWith(() => new TweenInformation<T>(Time.time, start(), finish(), duration(), easeType, out startValue, out finishValue))
                                // Update のストリームに変換
                                .SelectMany(information => Observable.EveryUpdate().Do(_ => information.Time = Time.time - information.StartTime).Select(_ => information))
                                // Tween 時間が処理時間よりも小さい間流し続ける
                                .TakeWhile(information => information.Time <= information.Duration)
                                // start と finish を入れ替えて、実際の Easing 処理実行
                                .Select(information => Easing(information.Time, information.Finish, (information.Start - information.Finish), information.Duration, MIRROR_EASE_TYPE_MAP[information.EaseType]).Value)
                                // 最終フレームの値を確実に流すために OnCompleted が来たら最終値を一つ流すストリームに繋ぐ
                                .Concat(Observable.Create(returnStartValue).Take(1))
                        )
                        .Repeat();
                    break;
            }
            return stream;
        }

        private static OperatableBase<T> Easing<T>(float time, OperatableBase<T> initial, OperatableBase<T> delta, float duration, EaseType easeType) where T : struct {
            if (!EasingFunctions<T>.EASING_FUNCTION_MAP.ContainsKey(easeType)) {
                throw new ArgumentException(string.Format("EaseType: '{0}' does not implement yet.", easeType.ToString()));
            }
            if (time <= 0.0f) {
                return initial;
            }
            if (time >= duration) {
                return initial + delta;
            }
            return EasingFunctions<T>.EASING_FUNCTION_MAP[easeType](time, initial, delta, duration);
        }

        private static class EasingFunctions<T> where T : struct {

            private const float EASE_BACK_THRESHOLD = 1.70158f;

            public static readonly Dictionary<EaseType, Func<float, OperatableBase<T>, OperatableBase<T>, float, OperatableBase<T>>> EASING_FUNCTION_MAP = new Dictionary<EaseType, Func<float, OperatableBase<T>, OperatableBase<T>, float, OperatableBase<T>>>() {
                { EaseType.Linear, EaseLinear },
                { EaseType.InQuadratic, EaseInQuadratic },
                { EaseType.OutQuadratic, EaseOutQuadratic },
                { EaseType.InOutQuadratic, EaseInOutQuadratic },
                { EaseType.InCubic, EaseInCubic },
                { EaseType.OutCubic, EaseOutCubic },
                { EaseType.InOutCubic, EaseInOutCubic },
                { EaseType.InQuartic, EaseInQuartic },
                { EaseType.OutQuartic, EaseOutQuartic },
                { EaseType.InOutQuartic, EaseInOutQuartic },
                { EaseType.InQuintic, EaseInQuintic },
                { EaseType.OutQuintic, EaseOutQuintic },
                { EaseType.InOutQuintic, EaseInOutQuintic },
                { EaseType.InSinusoidal, EaseInSinusoidal },
                { EaseType.OutSinusoidal, EaseOutSinusoidal },
                { EaseType.InOutSinusoidal, EaseInOutSinusoidal },
                { EaseType.InExponential, EaseInExponential },
                { EaseType.OutExponential, EaseOutExponential },
                { EaseType.InOutExponential, EaseInOutExponential },
                { EaseType.InCircular, EaseInCircular },
                { EaseType.OutCircular, EaseOutCircular },
                { EaseType.InOutCircular, EaseInOutCircular },
                { EaseType.InBack, EaseInBack },
                { EaseType.OutBack, EaseOutBack },
                { EaseType.InOutBack, EaseInOutBack },
                { EaseType.InBounce, EaseInBounce },
                { EaseType.OutBounce, EaseOutBounce },
                { EaseType.InOutBounce, EaseInOutBounce },
                { EaseType.InElastic, EaseInElastic },
                { EaseType.OutElastic, EaseOutElastic },
                { EaseType.InOutElastic, EaseInOutElastic },
            };

            private static OperatableBase<T> EaseLinear(float time, OperatableBase<T> initial, OperatableBase<T> delta, float duration) {
                return delta * time / duration + initial;
            }

            private static OperatableBase<T> EaseInQuadratic(float time, OperatableBase<T> initial, OperatableBase<T> delta, float duration) {
                time /= duration;
                return delta * time * time + initial;
            }

            private static OperatableBase<T> EaseOutQuadratic(float time, OperatableBase<T> initial, OperatableBase<T> delta, float duration) {
                time /= duration;
                return -delta * time * (time - 2.0f) + initial;
            }

            private static OperatableBase<T> EaseInOutQuadratic(float time, OperatableBase<T> initial, OperatableBase<T> delta, float duration) {
                time /= duration / 2.0f;
                if (time <= 1.0f) {
                    return delta / 2.0f * time * time + initial;
                }
                time -= 1.0f;
                return -delta / 2.0f * (time * (time - 2.0f) - 1.0f) + initial;
            }

            private static OperatableBase<T> EaseInCubic(float time, OperatableBase<T> initial, OperatableBase<T> delta, float duration) {
                time /= duration;
                return delta * Mathf.Pow(time, 3.0f) + initial;
            }

            private static OperatableBase<T> EaseOutCubic(float time, OperatableBase<T> initial, OperatableBase<T> delta, float duration) {
                time /= duration;
                time = time - 1.0f;
                return delta * (Mathf.Pow(time, 3.0f) + 1.0f) + initial;
            }

            private static OperatableBase<T> EaseInOutCubic(float time, OperatableBase<T> initial, OperatableBase<T> delta, float duration) {
                time /= duration / 2.0f;
                if (time <= 1.0f) {
                    return delta / 2.0f * Mathf.Pow(time, 3.0f) + initial;
                }
                time -= 2.0f;
                return delta / 2.0f * (Mathf.Pow(time, 3.0f) + 2.0f) + initial;
            }

            private static OperatableBase<T> EaseInQuartic(float time, OperatableBase<T> initial, OperatableBase<T> delta, float duration) {
                time /= duration;
                return delta * Mathf.Pow(time, 4.0f) + initial;
            }

            private static OperatableBase<T> EaseOutQuartic(float time, OperatableBase<T> initial, OperatableBase<T> delta, float duration) {
                time /= duration;
                time -= 1.0f;
                return -delta * (Mathf.Pow(time, 4.0f) - 1.0f) + initial;
            }

            private static OperatableBase<T> EaseInOutQuartic(float time, OperatableBase<T> initial, OperatableBase<T> delta, float duration) {
                time /= duration / 2.0f;
                if (time <= 1.0f) {
                    return delta / 2.0f * Mathf.Pow(time, 4.0f) + initial;
                }
                time -= 2.0f;
                return -delta * 2.0f * (Mathf.Pow(time, 4.0f) - 2.0f) + initial;
            }

            private static OperatableBase<T> EaseInQuintic(float time, OperatableBase<T> initial, OperatableBase<T> delta, float duration) {
                time /= duration;
                return delta * Mathf.Pow(time, 5.0f) + initial;
            }

            private static OperatableBase<T> EaseOutQuintic(float time, OperatableBase<T> initial, OperatableBase<T> delta, float duration) {
                time /= duration;
                time -= 1.0f;
                return delta * (Mathf.Pow(time, 5.0f) + 1.0f) + initial;
            }

            private static OperatableBase<T> EaseInOutQuintic(float time, OperatableBase<T> initial, OperatableBase<T> delta, float duration) {
                time /= duration / 2.0f;
                if (time <= 1.0f) {
                    return delta / 2.0f * Mathf.Pow(time, 5.0f) + initial;
                }
                time -= 2.0f;
                return delta / 2.0f * (Mathf.Pow(time, 5.0f) + 2.0f) + initial;
            }

            private static OperatableBase<T> EaseInSinusoidal(float time, OperatableBase<T> initial, OperatableBase<T> delta, float duration) {
                return -delta * Mathf.Cos(time / duration * (Mathf.PI / 2.0f)) + delta + initial;
            }

            private static OperatableBase<T> EaseOutSinusoidal(float time, OperatableBase<T> initial, OperatableBase<T> delta, float duration) {
                return delta * Mathf.Sin(time / duration * (Mathf.PI / 2.0f)) + initial;
            }

            private static OperatableBase<T> EaseInOutSinusoidal(float time, OperatableBase<T> initial, OperatableBase<T> delta, float duration) {
                return -delta / 2.0f * (Mathf.Cos(Mathf.PI * time / duration) + 1.0f) + initial;
            }

            private static OperatableBase<T> EaseInExponential(float time, OperatableBase<T> initial, OperatableBase<T> delta, float duration) {
                return delta * Mathf.Pow(2.0f, 10.0f * (time / duration - 1.0f)) + initial;
            }

            private static OperatableBase<T> EaseOutExponential(float time, OperatableBase<T> initial, OperatableBase<T> delta, float duration) {
                return delta * (-Mathf.Pow(2.0f, -10.0f * time / duration) + 1.0f) + initial;
            }

            private static OperatableBase<T> EaseInOutExponential(float time, OperatableBase<T> initial, OperatableBase<T> delta, float duration) {
                time /= duration / 2.0f;
                if (time <= 1.0f) {
                    return delta / 2.0f * Mathf.Pow(2.0f, 10.0f * (time - 1.0f)) + initial;
                }
                time -= 1.0f;
                return delta / 2.0f * (-Mathf.Pow(2.0f, -10.0f * time) + 2.0f) + initial;
            }

            private static OperatableBase<T> EaseInCircular(float time, OperatableBase<T> initial, OperatableBase<T> delta, float duration) {
                time /= duration;
                return -delta * (Mathf.Sqrt(1 - Mathf.Pow(time, 2.0f)) + 1.0f) + initial;
            }

            private static OperatableBase<T> EaseOutCircular(float time, OperatableBase<T> initial, OperatableBase<T> delta, float duration) {
                time /= duration;
                time -= 1.0f;
                return delta * Mathf.Sqrt(1 - Mathf.Pow(time, 2.0f)) + initial;
            }

            private static OperatableBase<T> EaseInOutCircular(float time, OperatableBase<T> initial, OperatableBase<T> delta, float duration) {
                time /= duration / 2.0f;
                if (time <= 1.0f) {
                    return -delta / 2.0f * (Mathf.Sqrt(1 - Mathf.Pow(time, 2.0f)) - 1.0f) + initial;
                }
                time -= 2.0f;
                return delta / 2.0f * (Mathf.Sqrt(1 - Mathf.Pow(time, 2.0f)) + 1.0f) + initial;
            }

            private static OperatableBase<T> EaseInBack(float time, OperatableBase<T> initial, OperatableBase<T> delta, float duration) {
                time /= duration;
                return delta * Mathf.Pow(time, 2.0f) * ((EASE_BACK_THRESHOLD + 1.0f) * time - EASE_BACK_THRESHOLD) + initial;
            }

            private static OperatableBase<T> EaseOutBack(float time, OperatableBase<T> initial, OperatableBase<T> delta, float duration) {
                time /= duration;
                time -= 1.0f;
                return delta * (Mathf.Pow(time, 2.0f) * ((EASE_BACK_THRESHOLD + 1.0f) * time + EASE_BACK_THRESHOLD) + 1.0f) + initial;
            }

            private static OperatableBase<T> EaseInOutBack(float time, OperatableBase<T> initial, OperatableBase<T> delta, float duration) {
                const float threshold = EASE_BACK_THRESHOLD * 1.525f;
                time /= duration / 2.0f;
                if (time <= 1.0f) {
                    return delta / 2.0f * (Mathf.Pow(time, 2.0f) * ((threshold + 1.0f) * time - threshold)) + initial;
                }
                time -= 2.0f;
                return delta / 2.0f * (Mathf.Pow(time, 2.0f) * ((threshold + 1.0f) * time + threshold) + 2.0f) + initial;
            }

            private static OperatableBase<T> EaseInBounce(float time, OperatableBase<T> initial, OperatableBase<T> delta, float duration) {
                return delta - EaseOutBounce(duration - time, default(OperatableBase<T>), delta, duration) + initial;
            }

            private static OperatableBase<T> EaseOutBounce(float time, OperatableBase<T> initial, OperatableBase<T> delta, float duration) {
                time /= duration;
                if (time <= (1.0f / 2.75f)) {
                    return delta * (7.5625f * Mathf.Pow(time, 2.0f)) + initial;
                }
                if (time <= (2.0f / 2.75f)) {
                    time -= (1.5f / 2.75f);
                    return delta * (7.5625f * Mathf.Pow(time, 2.0f) + 0.75f) + initial;
                }
                if (time <= (2.5f / 2.75f)) {
                    time -= (2.25f / 2.75f);
                    return delta * (7.5625f * Mathf.Pow(time, 2.0f) + 0.9375f) + initial;
                }
                time -= (2.625f / 2.75f);
                return delta * (7.5625f * Mathf.Pow(time, 2.0f) + 0.984375f) + initial;
            }

            private static OperatableBase<T> EaseInOutBounce(float time, OperatableBase<T> initial, OperatableBase<T> delta, float duration) {
                if (time <= duration / 2.0f) {
                    return EaseInBounce(time * 2.0f, default(OperatableBase<T>), delta, duration) * 0.5f + initial;
                }
                return EaseOutBounce(time * 2.0f - duration, default(OperatableBase<T>), delta, duration) * 0.5f + delta * 0.5f + initial;
            }

            private static OperatableBase<T> EaseInElastic(float time, OperatableBase<T> initial, OperatableBase<T> delta, float duration) {
                time /= duration;
                if (Mathf.Approximately(time, 1.0f)) {
                    return initial + delta;
                }
                time -= 1.0f;
                float p = duration * 0.3f;
                float s = p / 4.0f;
                return -(delta * Mathf.Pow(2.0f, 10.0f * time) * Mathf.Sin((time * duration - s) * (2.0f * Mathf.PI) / p)) + initial;
            }

            private static OperatableBase<T> EaseOutElastic(float time, OperatableBase<T> initial, OperatableBase<T> delta, float duration) {
                time /= duration;
                if (Mathf.Approximately(time, 1.0f)) {
                    return initial + delta;
                }
                float p = duration * 0.3f;
                float s = p / 4.0f;
                return delta * Mathf.Pow(2.0f, -10.0f * time) * Mathf.Sin((time * duration - s) * (2.0f * Mathf.PI) / p) + delta + initial;
            }

            private static OperatableBase<T> EaseInOutElastic(float time, OperatableBase<T> initial, OperatableBase<T> delta, float duration) {
                time /= duration / 2.0f;
                if (Mathf.Approximately(time, 2.0f)) {
                    return initial + delta;
                }
                time -= 1.0f;
                float p = duration * (0.3f * 1.5f);
                float s = p / 4.0f;
                if (time <= 0.0f) {
                    return (delta * Mathf.Pow(2.0f, 10.0f * time) * Mathf.Sin((time * duration - s) * (2.0f * Mathf.PI) / p)) * -0.5f + initial;
                }
                return delta * Mathf.Pow(2.0f, -10.0f * time) * Mathf.Sin((time * duration - s) * (2.0f * Mathf.PI) / p) * 0.5f + delta + initial;
            }

        }

        // XXX: 本当は struct にした方がコストが低いが、 abstrcat クラスで operator を定義することで記述を柔軟にしたかったので class 定義にしている
        private abstract class OperatableBase<T> where T : struct {

            public abstract T Value { get; set; }

            protected abstract OperatableBase<T> Add(OperatableBase<T> value);

            protected abstract OperatableBase<T> Substract(OperatableBase<T> value);

            protected abstract OperatableBase<T> Multiply(float value);

            protected abstract OperatableBase<T> Divide(float value);

            protected abstract int Compare(OperatableBase<T> value);

            public static OperatableBase<T> operator +(OperatableBase<T> a, OperatableBase<T> b) {
                return a.Add(b);
            }

            public static OperatableBase<T> operator -(OperatableBase<T> a, OperatableBase<T> b) {
                return a.Substract(b);
            }

            public static OperatableBase<T> operator -(OperatableBase<T> a) {
                return a.Multiply(-1.0f);
            }

            public static OperatableBase<T> operator *(OperatableBase<T> a, float b) {
                return a.Multiply(b);
            }

            public static OperatableBase<T> operator /(OperatableBase<T> a, float b) {
                return a.Divide(b);
            }

            public static bool operator <(OperatableBase<T> a, OperatableBase<T> b) {
                return a.Compare(b) > 0;
            }

            public static bool operator >(OperatableBase<T> a, OperatableBase<T> b) {
                return a.Compare(b) < 0;
            }

        }

        private class OperatableInt : OperatableBase<int> {

            public sealed override int Value { get; set; }

            protected override OperatableBase<int> Add(OperatableBase<int> value) {
                return new OperatableInt(this.Value + value.Value);
            }

            protected override OperatableBase<int> Substract(OperatableBase<int> value) {
                return new OperatableInt(this.Value - value.Value);
            }

            protected override OperatableBase<int> Multiply(float value) {
                return new OperatableInt((int)(this.Value * value));
            }

            protected override OperatableBase<int> Divide(float value) {
                return new OperatableInt((int)(this.Value / value));
            }

            protected override int Compare(OperatableBase<int> value) {
                return this.Value > value.Value ? 1 : -1;
            }

            public OperatableInt(int value) {
                this.Value = value;
            }

        }

        private class OperatableFloat : OperatableBase<float> {

            public sealed override float Value { get; set; }

            protected override OperatableBase<float> Add(OperatableBase<float> value) {
                return new OperatableFloat(this.Value + value.Value);
            }

            protected override OperatableBase<float> Substract(OperatableBase<float> value) {
                return new OperatableFloat(this.Value - value.Value);
            }

            protected override OperatableBase<float> Multiply(float value) {
                return new OperatableFloat(this.Value * value);
            }

            protected override OperatableBase<float> Divide(float value) {
                return new OperatableFloat(this.Value / value);
            }

            protected override int Compare(OperatableBase<float> value) {
                return this.Value > value.Value ? 1 : -1;
            }

            public OperatableFloat(float value) {
                this.Value = value;
            }

        }

        private class OperatableVector2 : OperatableBase<Vector2> {

            public sealed override Vector2 Value { get; set; }

            protected override OperatableBase<Vector2> Add(OperatableBase<Vector2> value) {
                return new OperatableVector2(this.Value + value.Value);
            }

            protected override OperatableBase<Vector2> Substract(OperatableBase<Vector2> value) {
                return new OperatableVector2(this.Value - value.Value);
            }

            protected override OperatableBase<Vector2> Multiply(float value) {
                return new OperatableVector2(this.Value * value);
            }

            protected override OperatableBase<Vector2> Divide(float value) {
                return new OperatableVector2(this.Value / value);
            }

            protected override int Compare(OperatableBase<Vector2> value) {
                return this.Value.magnitude > value.Value.magnitude ? 1 : -1;
            }

            public OperatableVector2(Vector2 value) {
                this.Value = value;
            }

        }

        private class OperatableVector3 : OperatableBase<Vector3> {

            public sealed override Vector3 Value { get; set; }

            protected override OperatableBase<Vector3> Add(OperatableBase<Vector3> value) {
                return new OperatableVector3(this.Value + value.Value);
            }

            protected override OperatableBase<Vector3> Substract(OperatableBase<Vector3> value) {
                return new OperatableVector3(this.Value - value.Value);
            }

            protected override OperatableBase<Vector3> Multiply(float value) {
                return new OperatableVector3(this.Value * value);
            }

            protected override OperatableBase<Vector3> Divide(float value) {
                return new OperatableVector3(this.Value / value);
            }

            protected override int Compare(OperatableBase<Vector3> value) {
                return this.Value.magnitude > value.Value.magnitude ? 1 : -1;
            }

            public OperatableVector3(Vector3 value) {
                this.Value = value;
            }

        }

    }

}