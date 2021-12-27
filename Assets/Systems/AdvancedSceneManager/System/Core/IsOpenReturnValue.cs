namespace AdvancedSceneManager
{

    namespace Models
    {

        /// <summary>
        /// <para>A struct to make working with the return value of the IsOpen() function a bit easier.</para>
        /// <para>Implicitly casts to bool, which can be used to check if scene is open in either.</para>
        /// </summary>
        public struct IsOpenReturnValue
        {

            /// <summary>The scene was opened as part of a collection.</summary>
            public bool withCollection;
            /// <summary>The scene was opened as standalone.</summary>
            public bool asStandalone;
            /// <summary>The scene is currently preloaded.</summary>
            public bool isPreloaded;

            public static implicit operator bool(IsOpenReturnValue value) =>
                value.withCollection | value.asStandalone;

            public static implicit operator IsOpenReturnValue((bool withCollection, bool asStandalone) value) =>
                new IsOpenReturnValue() { withCollection = value.withCollection, asStandalone = value.asStandalone };

            public static implicit operator IsOpenReturnValue((bool withCollection, bool asStandalone, bool isPreloaded) value) =>
                new IsOpenReturnValue() { withCollection = value.withCollection, asStandalone = value.asStandalone, isPreloaded = value.isPreloaded };

        }

    }

}
