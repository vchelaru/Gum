using Gum;
using Gum.DataTypes;
using Gum.Wireframe;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using System.Runtime.CompilerServices;
using System;
using System.Collections.Generic;
using System.Linq;

using Gum.DataTypes.Variables;
using Gum.Managers;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Gum.Collections;


#if !FRB
using Gum.StateAnimation.Runtime;
#endif

namespace GumRuntime
{
    public static class ElementSaveExtensions
    {
        // The element-type table is layered: ElementTypeRegistry.Base holds registrations made outside
        // any service (module initializers, startup code), and each GumContext holds the ones made while
        // its service was active. Writes go to the active context's layer, else the base. Reads see the
        // current context's layer plus the base, newest registration first (#6066).
        private static ElementTypeRegistry WriteLayer =>
            GumContext.RegisteringShared ? ElementTypeRegistry.Base : GumContext.Current?.ElementTypes ?? ElementTypeRegistry.Base;
        private static ElementTypeRegistry? ReadLayer => GumContext.Current?.ElementTypes;

        /// <summary>
        /// Evaluates a variable reference's right side. The 4th argument is the live,
        /// already-laid-out <see cref="GraphicalUiElement"/> that owns the reference (null when
        /// applying against pure <see cref="StateSave"/> data with no live tree, e.g. the tool's
        /// design-time apply path with no wireframe rendered) - it lets the evaluator resolve
        /// runtime-computed identifiers like <c>AbsoluteWidth</c> that don't exist on authored data.
        /// </summary>
        public static Func<StateSave, string, string?, GraphicalUiElement?, object?>? CustomEvaluateExpression;

        // The value Gum.Expressions evaluated but refused to cast to the desired type, left here by the evaluator behind
        // CustomEvaluateExpression (which can only return null) so the reference can say why it was not applied (#6463).
        // TryReadReference clears it before each evaluation and reads it right after, so an evaluator that does not set it
        // simply reports nothing. It is a side channel, not a second evaluation: a live row that is refused every frame
        // must not evaluate twice. Evaluators must not nest: a wrapper that evaluates a second expression inside
        // CustomEvaluateExpression makes the outer reference report the inner value.
#nullable enable
        [ThreadStatic]
        internal static object? RejectedCastValue;
#nullable restore

        /// <summary>
        /// Enumerates every value a reference's right side could resolve to (all ternary
        /// branches, not just the one the condition currently selects). Same arguments as
        /// <see cref="CustomEvaluateExpression"/>. Set by <c>GumExpressionService.Initialize()</c>;
        /// used by <see cref="GetAllVariableReferenceBranches"/> for font bulk-generation
        /// (see issue #4042) so it can pregenerate every branch of a conditional Font-affecting
        /// expression instead of only the one active at collection time.
        /// </summary>
        public static Func<StateSave, string, string?, GraphicalUiElement?, IEnumerable<object>>? CustomEvaluateExpressionAllBranches;

        /// <summary>
        /// Associates an element name with a strongly-typed <see cref="GraphicalUiElement"/> subclass
        /// (typically a codegen'd <c>*Runtime</c> class) that Gum instantiates reflectively whenever
        /// that element is built from its <see cref="ElementSave"/>.
        /// </summary>
        /// <remarks>
        /// The <c>DynamicallyAccessedMembers</c> annotation on <paramref name="gueInheritingType"/> is
        /// what keeps this working under <c>PublishTrimmed</c>/<c>PublishAot</c> (issue #4318).
        /// Callers pass a literal <c>typeof(X)</c>, so the trimmer follows the annotation and preserves
        /// X's constructors - <c>typeof(X)</c> on its own roots the type but not its members, and the
        /// <c>(bool, bool)</c> constructor this registry needs is otherwise referenced by nothing.
        /// Prefer <see cref="RegisterGueInstantiation{T}"/>, which needs no reflection at all.
        /// </remarks>
        public static void RegisterGueInstantiationType(
            string elementName,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
            Type gueInheritingType,
            bool overwriteIfAlreadyExists = true)
        {
            if(overwriteIfAlreadyExists)
            {
                WriteLayer.SetType(elementName, gueInheritingType);
            }
            else
            {
                if(IsRegistered(elementName) == false)
                {
                    WriteLayer.SetType(elementName, gueInheritingType);
                }
            }
        }

        /// <summary>
        /// Registers a factory that produces a strongly-typed <see cref="GraphicalUiElement"/>
        /// wrapper (e.g. <c>ContainerRuntime</c>, <c>SpriteRuntime</c>, <c>CircleRuntime</c>)
        /// for the given base-type name. This is the <b>primary</b> way Gum builds the visual
        /// tree: the wrapper's constructor installs its own inner renderable via
        /// <c>SetContainedObject</c>, so its rendering, layout, and behavior are fully its own.
        /// </summary>
        /// <remarks>
        /// Prefer this over <see cref="CustomCreateGraphicalComponentFunc"/> for every new
        /// base type. The custom-func path is a fallback for cases where no strongly-typed
        /// wrapper has been registered; see <c>Gum.Wireframe.FallbackRenderableFactory</c>
        /// and <see href="https://github.com/vchelaru/Gum/issues/2915">issue #2915</see> for
        /// the architectural background.
        /// </remarks>
        public static void RegisterGueInstantiation<T>(string elementName, Func<T> templateFunc) where T : GraphicalUiElement
        {
            WriteLayer.SetFunc(elementName, templateFunc);
        }

        private static bool IsRegistered(string elementName)
        {
            return ElementTypeRegistry.Base.ContainsName(elementName)
                || (ReadLayer?.ContainsName(elementName) ?? false);
        }


        public static void RegisterDefaultInstantiationType<T>(Func<T> templateFunc) where T : GraphicalUiElement
        {
            WriteLayer.SetTemplate(templateFunc);
        }

        /// <summary>
        /// Removes the element-name registrations made through
        /// <see cref="RegisterGueInstantiationType"/> whose registered type matches
        /// <paramref name="shouldRemove"/>.
        /// </summary>
        public static void UnregisterGueInstantiationTypes(Func<Type, bool> shouldRemove)
        {
            ElementTypeRegistry.Base.RemoveTypes(shouldRemove);
            ReadLayer?.RemoveTypes(shouldRemove);
        }

        /// <summary>
        /// Clears the current service's registrations. The process-wide registrations and the shared
        /// hooks below are cleared only when no other service is still live, so one service's
        /// Uninitialize does not unregister the types another service builds with.
        /// </summary>
        public static void ClearRegistrations()
        {
            ReadLayer?.Clear();

            if (!GumContext.HasLiveContextOtherThan(GumContext.Current))
            {
                ElementTypeRegistry.Base.Clear();
                CustomCreateGraphicalComponentFunc = null;
                CustomEvaluateExpression = null;
                CustomEvaluateExpressionAllBranches = null;
            }
        }



        public static GraphicalUiElement CreateGueForElement(ElementSave elementSave, bool fullInstantiation = false, string? genericType = null)
        {
#if FULL_DIAGNOSTICS
            if (elementSave == null)
            {
                throw new ArgumentNullException(nameof(elementSave));
            }
            if (elementSave.Name == null)
            {
                throw new InvalidOperationException("The argument ElementSave must have a name");
            }

#endif
            GraphicalUiElement? toReturn = null;

            toReturn = TryCreateStrongTypeForElement(elementSave, fullInstantiation, genericType);

            if (toReturn == null)
            {
                Func<GraphicalUiElement>? templateFunc = ElementTypeRegistry.GetTemplate(ReadLayer);
                if (templateFunc != null)
                {
                    toReturn = templateFunc();
                }
            }

            if (toReturn == null)
            {
                toReturn = new GraphicalUiElement();
            }


            toReturn.ElementSave = elementSave;
            return toReturn;
        }

        // ElementTypeRegistry stores plain Types, which cannot carry the
        // DynamicallyAccessedMembers annotation that RegisterGueInstantiationType declares on the way
        // in. The constructors are preserved at each registration site, so reading them back out is
        // safe even though the annotation does not survive the round trip.
        [UnconditionalSuppressMessage("Trimming", "IL2072",
            Justification = "Types reach this dictionary only via RegisterGueInstantiationType, whose parameter is annotated with PublicConstructors.")]
        private static GraphicalUiElement? TryCreateStrongTypeForElement(ElementSave elementSave, bool fullInstantiation, string? genericType)
        {
            GraphicalUiElement? toReturn = null;
            var elementName = elementSave.Name;
            var attemptedGenericLookup = false;
            if (!string.IsNullOrEmpty(genericType))
            {
                elementName = elementName + "<T>";
                attemptedGenericLookup = true;
            }

            ElementTypeRegistry? layer = ReadLayer;

            if (ElementTypeRegistry.TryGetFunc(layer, elementName, out Func<GraphicalUiElement>? registeredFunc))
            {
                toReturn = registeredFunc();
            }
            else if (ElementTypeRegistry.TryGetType(layer, elementName, out Type? registeredType))
            {
                // This code allows systems (like games that use Gum) to assign types
                // to their GraphicalUiElements so that users of the code can work with
                // strongly-typed Gum objects.
                var type = registeredType;

                if (!string.IsNullOrEmpty(genericType))
                {
                    if (!ElementTypeRegistry.TryGetType(layer, genericType!, out Type? genericArgument))
                    {
                        throw new KeyNotFoundException($"No runtime type is registered for {genericType}");
                    }
                    type = MakeGenericRuntimeType(type, genericArgument);
                }

                toReturn = InstantiateRegisteredType(type, elementName, fullInstantiation);
            }
            else if (attemptedGenericLookup)
            {
                // fall back to non-generic lookup if the generic type was not registered
                elementName = elementSave.Name;
                if (ElementTypeRegistry.TryGetFunc(layer, elementName, out Func<GraphicalUiElement>? fallbackFunc))
                {
                    toReturn = fallbackFunc();
                }
                else if (ElementTypeRegistry.TryGetType(layer, elementName, out Type? fallbackType))
                {
                    toReturn = InstantiateRegisteredType(fallbackType, elementName, fullInstantiation);
                }
            }

            if (toReturn == null)
            {
                // try inheritance:
                var baseTypeName = elementSave.BaseType;

                if(!string.IsNullOrEmpty(baseTypeName))
                {
                    var baseElement = ObjectFinder.Self.GetElementSave(baseTypeName);

                    if(baseElement != null)
                    {
                        toReturn = TryCreateStrongTypeForElement(baseElement, fullInstantiation, genericType);
                    }
                    // If it has a base type, but we can't resolve it, it's an error.
                    // We can still create something useful to avoid falling apart here:
                    toReturn = toReturn ?? new GraphicalUiElement(new InvisibleRenderable());
                }
            }

            return toReturn;
        }

        /// <summary>
        /// Builds a registered runtime type, preferring the <c>(bool fullInstantiation, bool
        /// tryCreateFormsObject)</c> constructor that codegen'd runtimes declare and falling back to a
        /// parameterless one (InteractiveGue in MonoGame Gum).
        /// </summary>
        /// <remarks>
        /// The suppressions are safe because every <paramref name="type"/> reaching this method was
        /// registered through <see cref="RegisterGueInstantiationType"/>, whose parameter carries
        /// <see cref="DynamicallyAccessedMemberTypes.PublicConstructors"/>; the trimmer preserves the
        /// constructors at each registration site. Only the round-trip through an unannotated
        /// <c>Dictionary&lt;string, Type&gt;</c> loses that annotation, which is what the analyzer is
        /// reporting.
        /// </remarks>
        private static GraphicalUiElement? InstantiateRegisteredType(
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
            Type type,
            string elementName,
            bool fullInstantiation)
        {
            var constructorWithArgs = type.GetConstructor(new Type[] { typeof(bool), typeof(bool) });
            if (constructorWithArgs != null)
            {
                return constructorWithArgs.Invoke(new object[] { fullInstantiation, true }) as GraphicalUiElement;
            }

            // For InteractiveGue in MonoGame Gum
            if (type.GetConstructor(Type.EmptyTypes) != null)
            {
                return (GraphicalUiElement)Activator.CreateInstance(type)!;
            }

            throw new InvalidOperationException(
                $"The runtime type {type.Name}, registered for the Gum element \"{elementName}\", has neither a " +
                "(bool fullInstantiation, bool tryCreateFormsObject) constructor nor a parameterless one, so Gum " +
                "cannot create it." + Environment.NewLine +
                "If this is a trimmed or Native AOT build and the type does declare that constructor in source, " +
                "the trimmer removed it because nothing else in the app calls it. Rebuild against a Gum version " +
                "that annotates RegisterGueInstantiationType (issue #4318), or add " +
                $"<TrimmerRootAssembly Include=\"{type.Assembly.GetName().Name}\" /> to the project as a workaround.");
        }

        /// <remarks>
        /// Generic Gum components need a runtime-constructed closed type. Native AOT can only supply
        /// one when the instantiation is statically reachable, so this stays a documented limitation
        /// rather than something the annotation above can fix.
        /// </remarks>
        [UnconditionalSuppressMessage("Trimming", "IL2055",
            Justification = "Generic Gum components are not supported under Native AOT; the failure is reported at runtime with a targeted message.")]
        [UnconditionalSuppressMessage("AOT", "IL3050",
            Justification = "Generic Gum components are not supported under Native AOT; the failure is reported at runtime with a targeted message.")]
        private static Type MakeGenericRuntimeType(Type openType, Type argumentType)
        {
            try
            {
                return openType.MakeGenericType(argumentType);
            }
            // PlatformNotSupportedException derives from NotSupportedException, so this covers both
            // shapes ILC can throw for an instantiation it could not precompile.
            catch (NotSupportedException exception)
            {
                throw new InvalidOperationException(
                    $"Gum could not create the generic runtime type {openType.Name}<{argumentType.Name}>. Generic Gum " +
                    "components require the closed type to be constructed at runtime, which Native AOT does not support " +
                    "unless that exact instantiation is already reachable from code.", exception);
            }
        }

        public static GraphicalUiElement ToGraphicalUiElement(this ElementSave elementSave,
            ISystemManagers systemManagers,
            bool addToManagers, string? genericType = null)
        {
            // With two or more services, set IGumService.Default to the service these managers belong to before
            // calling this: the build resolves its registrations and project through the default.
            bool isOutermostBuild = _buildDepth == 0;
            if (isOutermostBuild)
            {
                ApplyReferencesOfProjectAwaitingFirstBuild();
            }
            _buildDepth++;
            try
            {
                return ToGraphicalUiElementCore(elementSave, systemManagers, addToManagers, genericType, isOutermostBuild);
            }
            finally
            {
                _buildDepth--;
            }
        }

        // Projects a service loaded from files, whose references have not been applied yet. A file edited by hand
        // (a text editor, a git merge) leaves a reader holding the value it was saved with, and a hot reload shows
        // the end of the chain (#6853), so a fresh load does too (#6881). Applied at the first build rather than at
        // load because the game installs Gum.Expressions after it initializes Gum, and the rows need it to evaluate.
        private static readonly ConditionalWeakTable<GumProjectSave, object> ProjectsAwaitingFirstBuild =
            new ConditionalWeakTable<GumProjectSave, object>();

        /// <summary>
        /// Applies <see cref="ApplyAllVariableReferences"/> to the project the first time a visual is built from it.
        /// Called by each service after it loads a project from files.
        /// </summary>
        internal static void ApplyAllVariableReferencesBeforeFirstBuild(this GumProjectSave project)
        {
            ProjectsAwaitingFirstBuild.AddOrUpdate(project, project);
        }

        private static void ApplyReferencesOfProjectAwaitingFirstBuild()
        {
            GumProjectSave? project = ObjectFinder.Self.GumProjectSave;
            if (project != null && ProjectsAwaitingFirstBuild.Remove(project))
            {
                project.ApplyAllVariableReferences();
            }
        }

        // Nesting depth of ToGraphicalUiElement; only the outermost build stamps its owner. A plain static:
        // all UI work runs on one thread.
        private static int _buildDepth;

        private static GraphicalUiElement ToGraphicalUiElementCore(ElementSave elementSave,
            ISystemManagers systemManagers, bool addToManagers, string? genericType, bool isOutermostBuild)
        {
            GraphicalUiElement toReturn = CreateGueForElement(elementSave, genericType: genericType);

            toReturn.Name = elementSave.Name;

            if(toReturn.IsFullyCreated == false)
            {
                elementSave.SetGraphicalUiElement(toReturn, systemManagers);
            }

            //no layering support yet
            if (addToManagers)
            {
                toReturn.AddToManagers(systemManagers, null);
            }
            else if (isOutermostBuild && systemManagers?.Context is GumContext owner)
            {
                // Not attached, so there are no managers to find its service through later: remember it
                // (GumContext.OwnerOf) for font and source-file changes made afterward.
                GumContext.StampBuiltBy(toReturn, owner);
            }

            return toReturn;
        }

        [Obsolete("Use AddStatesAndCategoriesRecursivelyToGue since that more clearly indicates what the method is doing")]
        public static void SetStatesAndCategoriesRecursively(this GraphicalUiElement graphicalElement, ElementSave elementSave) => AddStatesAndCategoriesRecursivelyToGue(graphicalElement, elementSave);

        public static void AddStatesAndCategoriesRecursivelyToGue(this GraphicalUiElement graphicalElement, ElementSave elementSave)
        {
            if (graphicalElement == null)
            {
                throw new ArgumentNullException(nameof(graphicalElement));
            }
            if (!string.IsNullOrEmpty(elementSave.BaseType))
            {
                var baseElementSave = Gum.Managers.ObjectFinder.Self.GetElementSave(elementSave.BaseType);
                if (baseElementSave != null)
                {
                    graphicalElement.AddStatesAndCategoriesRecursivelyToGue(baseElementSave);
                }
            }

            // We need to set categories and states before calling SetGraphicalUiElement so that the states can be used
            foreach (var category in elementSave.Categories)
            {
                graphicalElement.AddCategory(category);
            }

            graphicalElement.AddStates(elementSave.States);
        }

        /// <summary>
        /// Backend-supplied fallback that produces the inner <see cref="IRenderable"/> for a
        /// <see cref="GraphicalUiElement"/> when the primary registration path
        /// (<see cref="RegisterGueInstantiation{T}"/>) did not already supply one. Runtime
        /// backends typically wire this to <c>Gum.Wireframe.FallbackRenderableFactory</c>
        /// (or a thin wrapper around it); the Gum tool wires its own plugin-aware
        /// implementation for editor wireframe preview.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This is the <b>secondary</b> path. It only fires from
        /// <see cref="CreateGraphicalComponent(GraphicalUiElement, ElementSave, ISystemManagers)"/>
        /// when a wrapper's <c>RenderableComponent</c> is still null after construction —
        /// rare for registered base types whose wrappers self-install their renderable.
        /// </para>
        /// <para>
        /// New base types should be added via <see cref="RegisterGueInstantiation{T}"/>, not
        /// by extending this delegate's backing factory. See <c>FallbackRenderableFactory</c>'s
        /// remarks and <see href="https://github.com/vchelaru/Gum/issues/2915">issue #2915</see>
        /// for why the two paths exist and the plan to collapse them.
        /// </para>
        /// </remarks>
        public static Func<string, ISystemManagers, IRenderable?>? CustomCreateGraphicalComponentFunc { get; set; }

        public static void CreateGraphicalComponent(this GraphicalUiElement graphicalElement, ElementSave elementSave, ISystemManagers systemManagers)
        {
            if (CustomCreateGraphicalComponentFunc == null)
            {
                throw new InvalidOperationException("The CustomCreateGraphicalComponentFunc must be set before calling CreateGraphicalComponent");
            }

            var containedObject = CustomCreateGraphicalComponentFunc(elementSave.Name, systemManagers);

            // experimental: The point of this is to give Screens a GrahpicalUiElement so that everyone can use .children
            if(containedObject == null && elementSave is ScreenSave && string.IsNullOrEmpty(elementSave.BaseType))
            {
                containedObject = new InvisibleRenderable();
                graphicalElement.WidthUnits = DimensionUnitType.RelativeToParent;
                graphicalElement.HeightUnits = DimensionUnitType.RelativeToParent;
                graphicalElement.Width = 0;
                graphicalElement.Height = 0;
                graphicalElement.Visible = true;
            }

            if (containedObject != null)
            {
                graphicalElement.SetContainedObject(containedObject);
            }
            else if (containedObject == null && !string.IsNullOrEmpty(elementSave.BaseType))
            {
                var baseElement = ObjectFinder.Self.GetElementSave(elementSave.BaseType);

                if (baseElement != null)
                {
                    CreateGraphicalComponent(graphicalElement, baseElement, systemManagers);
                }
            }
        }

        public static void AddExposedVariablesRecursively(this GraphicalUiElement graphicalElement, ElementSave elementSave)
        {
            if (!string.IsNullOrEmpty(elementSave.BaseType))
            {
                var baseElementSave = Gum.Managers.ObjectFinder.Self.GetElementSave(elementSave.BaseType);
                if (baseElementSave != null)
                {
                    graphicalElement.AddExposedVariablesRecursively(baseElementSave);
                }
            }


            if (elementSave != null)
            {
                foreach (var variable in elementSave.DefaultState!.Variables.Where(item => !string.IsNullOrEmpty(item.ExposedAsName)))
                {
                    graphicalElement.AddExposedVariable(variable.ExposedAsName!, variable.Name);
                }
            }

        }

        public static void SetVariablesRecursively(this GraphicalUiElement graphicalElement, ElementSave elementSave, Gum.DataTypes.Variables.StateSave stateSave)
        {
#if FULL_DIAGNOSTICS
            if (stateSave == null)
            {
                throw new ArgumentNullException(nameof(stateSave), "State cannot be null");
            }
            if(graphicalElement == null)
            {
                throw new ArgumentNullException(nameof(graphicalElement));
            }
#endif
            // This applies the element's references itself, in the order they are written, so the
            // variables it sets on the element do not also trigger live re-evaluation. Variables it
            // sets on its children do (see BeginReferenceLoad).
            graphicalElement.BeginReferenceLoad();
            try
            {
                if (!string.IsNullOrEmpty(elementSave.BaseType))
                {
                    var baseElementSave = Gum.Managers.ObjectFinder.Self.GetElementSave(elementSave.BaseType);
                    if (baseElementSave != null)
                    {
                        graphicalElement.SetVariablesRecursively(baseElementSave, baseElementSave.DefaultState!);
                    }
                }

                graphicalElement.ApplyState(stateSave);

                ApplyVariableReferences(graphicalElement, stateSave);

                // The line above reads every row's inputs from authored values, so a row that reads
                // another row's result sees it stale. Chains are evaluated again in dependency order, as
                // are rows a base component owns, which were evaluated before this element's values applied.
                if (stateSave == elementSave.DefaultState && elementSave is not StandardElementSave)
                {
                    VariableReferenceGraph graph = VariableReferenceGraph.GetFor(elementSave);
                    if (graph.HasChains || graph.HasInheritedRows)
                    {
                        graph.ApplyAll(elementSave, graphicalElement);
                    }
                }
            }
            finally
            {
                graphicalElement.EndReferenceLoad();
            }
        }

        public static bool ValueEquality(object? val1, object? val2)
        {
            if (val1 is string string1 && val2 is string string2)
            {
                return string1 == string2;
            }
            else
            {
                return val1?.Equals(val2) == true;
            }
        }

        // void VariableSet(ElementSave parentElement, InstanceSave instance, string changedMember, object oldValue,
        //     bool isFullCommit)
        public static Action<ElementSave, InstanceSave?, string, object?, bool>? VariableChangedThroughReference;

        /// <summary>
        /// Applies variable references on all elements in the project in dependency order.
        /// Elements that are referenced by others are applied first, so that downstream
        /// references pick up the latest values. Call this after modifying style variables
        /// to propagate changes across the entire project.
        /// </summary>
        public static void ApplyAllVariableReferences(this GumProjectSave project)
        {
            lock (ReportedWithoutOwner)
            {
                ReportedWithoutOwner.Clear();
            }

            List<ElementSave> allElements = new List<ElementSave>();
            allElements.AddRange(project.StandardElements);
            allElements.AddRange(project.Components);
            allElements.AddRange(project.Screens);

            // An element that reads another is applied after it. Elements in a cycle keep project order.
            List<ElementSave> sorted = ReferenceDependencies.OrderByDependency(allElements);

            // The rows do not change during the pass, so loops through other elements are looked up once.
            VariableReferenceGraph.BeginBatch();
            try
            {
                foreach (var element in sorted)
                {
                    foreach (var state in element.AllStates)
                    {
                        bool hasVariableReferences = false;
                        foreach (var variableList in state.VariableLists)
                        {
                            if (variableList.GetRootName() == "VariableReferences" && variableList.ValueAsIList.Count > 0)
                            {
                                hasVariableReferences = true;
                                break;
                            }
                        }

                        if (hasVariableReferences)
                        {
                            element.ApplyVariableReferences(state);
                        }
                    }
                }
            }
            finally
            {
                VariableReferenceGraph.EndBatch();
            }
        }

        /// <summary>
        /// Returns the list of states in this element (default + every categorized state)
        /// that have one or more <c>VariableReferences</c> rows whose left-hand-side scalars
        /// are not materialized into the state's <see cref="StateSave.Variables"/>. This
        /// indicates the state was authored by something other than the Gum tool's normal
        /// reference-edit path (AI agent, hand edit, programmatic creation, partial delete)
        /// and that running <c>ApplyVariableReferences</c> on each returned state
        /// would restore consistency. Commented-out (<c>//</c>) and unparseable lines are
        /// skipped; they are not treated as missing materializations.
        /// </summary>
        public static List<StateSave> GetStatesWithUnpropagatedReferences(this ElementSave element)
        {
            var result = new List<StateSave>();

            foreach (var state in element.AllStates)
            {
                if (StateHasUnpropagatedReferences(state))
                {
                    result.Add(state);
                }
            }

            return result;
        }

        private static bool StateHasUnpropagatedReferences(StateSave state)
        {
            foreach (var variableList in state.VariableLists)
            {
                if (variableList.GetRootName() != "VariableReferences" || variableList.ValueAsIList.Count == 0)
                {
                    continue;
                }

                var sourceObject = variableList.SourceObject;
                ElementSave? channelOwner = ResolveChannelOwner(state, sourceObject);

                foreach (string referenceString in variableList.ValueAsIList)
                {
                    if (string.IsNullOrWhiteSpace(referenceString) || referenceString.StartsWith("//"))
                    {
                        continue;
                    }

                    foreach (string expandedReferenceString in ExpandCompositeReferenceLine(referenceString, channelOwner))
                    {
                        var split = expandedReferenceString
                            .Split(equalsArray, 2, StringSplitOptions.RemoveEmptyEntries);
                        if (split.Length != 2)
                        {
                            continue;
                        }

                        var left = split[0].Trim();
                        var effectiveLeft = string.IsNullOrEmpty(sourceObject)
                            ? left
                            : $"{sourceObject}.{left}";

                        var hasMaterialized = state.Variables
                            .Any(v => v.Name == effectiveLeft);
                        if (!hasMaterialized)
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Resolves the <see cref="ElementSave"/> whose declared root variables determine whether a
        /// composite reference's channels (e.g. Red/Green/Blue for <c>Color</c>) are real for the
        /// object a <c>VariableReferences</c> row applies to - the row's own state's element when
        /// unqualified, or the qualified instance's type when <paramref name="sourceObject"/> is set.
        /// Public for the same reason as <see cref="ExpandCompositeReferenceLine"/>.
        /// </summary>
        public static ElementSave? ResolveChannelOwner(StateSave state, string? sourceObject)
        {
            ElementSave? element = state.ParentContainer;
            if (string.IsNullOrEmpty(sourceObject) || element == null)
            {
                return element;
            }

            InstanceSave? instance = element.GetInstance(sourceObject);
            return instance != null ? ObjectFinder.Self.GetElementSave(instance) : null;
        }

        /// <summary>
        /// Returns the set of qualified item names (e.g. <c>"TextInstance.FontSize"</c>
        /// or <c>"TextInstance.VariableReferences"</c>) owned by reachable categorized
        /// states on <paramref name="instance"/> in <paramref name="state"/>. "Owned"
        /// covers three shapes:
        ///   (a) Local <c>VariableReferences</c> rows on this state that target the instance
        ///       (their LHSes are scalar names owned by those refs).
        ///   (b) <c>VariableReferences</c> rows on the matched categorized state of the
        ///       instance's BaseType — both the LHSes (scalar names) AND a marker for
        ///       the list itself (<c>"&lt;instance&gt;.VariableReferences"</c>) so paste
        ///       can drop a base-snapshotted ref row that would shadow the categorized walk.
        ///   (c) Direct scalar variables on that matched categorized state — their names
        ///       are owned even without a reference, because the state's direct set would
        ///       otherwise be shadowed by a base-level scalar of the same name.
        /// The instance type's BaseType chain is walked via <c>GetStateSaveRecursively</c>.
        /// Used by copy/paste to filter base-element captures so the new instance picks
        /// up the active categorized state instead of inheriting the source's base-level
        /// orphans/overrides.
        /// </summary>
        public static HashSet<string> GetItemNamesOwnedByReachableCategorizedStates(this StateSave state, InstanceSave instance)
        {
            var result = new HashSet<string>(StringComparer.Ordinal);
            if (state == null || instance == null) return result;

            ElementSave? element = state.ParentContainer;
            if (element == null) return result;

            // (a) Local refs on this state targeting the instance.
            foreach (var variableList in state.VariableLists)
            {
                if (variableList.GetRootName() != "VariableReferences") continue;
                if (variableList.SourceObject != instance.Name) continue;
                AddReferenceLhses(variableList, instance.Name, result);
            }

            // (b) and (c): walk matched categorized states reached via state-variable assignments.
            ElementSave? instanceType = ObjectFinder.Self.GetElementSave(instance.BaseType);
            if (instanceType == null) return result;

            for (int i = 0; i < state.Variables.Count; i++)
            {
                var variable = state.Variables[i];
                if (!variable.SetsValue || variable.SourceObject != instance.Name) continue;
                if (!variable.IsState(element, out _, out StateSaveCategory? category)) continue;
                if (category == null) continue;

                string? stateName = variable.Value as string;
                if (string.IsNullOrEmpty(stateName)) continue;
                StateSave? matchedState = instanceType.GetStateSaveRecursively(stateName);
                if (matchedState == null) continue;

                // (b) refs on the matched state — LHSes + a marker for the row itself.
                foreach (var variableList in matchedState.VariableLists)
                {
                    if (variableList.GetRootName() != "VariableReferences") continue;
                    if (variableList.ValueAsIList.Count > 0)
                    {
                        // Marker: the matched state has a refs row, so a base-snapshotted
                        // VariableReferences row would shadow it. Drop the base row at paste.
                        result.Add($"{instance.Name}.VariableReferences");
                    }
                    AddReferenceLhses(variableList, instance.Name, result);
                }

                // (c) direct scalars on the matched state.
                for (int vi = 0; vi < matchedState.Variables.Count; vi++)
                {
                    var stateVar = matchedState.Variables[vi];
                    if (!stateVar.SetsValue) continue;
                    result.Add($"{instance.Name}.{stateVar.GetRootName()}");
                }
            }

            return result;
        }

        private static void AddReferenceLhses(VariableListSave variableList, string instanceName, HashSet<string> result)
        {
            foreach (var entry in variableList.ValueAsIList)
            {
                if (entry is not string referenceString) continue;
                if (string.IsNullOrWhiteSpace(referenceString) || referenceString.StartsWith("//")) continue;
                string[] split = referenceString.Split(equalsArray, 2, StringSplitOptions.RemoveEmptyEntries);
                if (split.Length != 2) continue;
                string left = split[0].Trim();
                if (string.IsNullOrEmpty(left)) continue;
                result.Add($"{instanceName}.{left}");
            }
        }

        // liveRoot: the live, already-laid-out GraphicalUiElement representing element, when one is
        // currently rendered (e.g. the tool's wireframe for the element being edited). Lets right-side
        // references resolve runtime-computed identifiers such as AbsoluteWidth; null (the default)
        // leaves those identifiers unresolved, same as before this parameter existed.
        // isFullCommit: reported to VariableChangedThroughReference so the tool can tell a committed
        // edit from an intermediate one produced while the user is still dragging.
        // notifyChanges: false skips VariableChangedThroughReference, for callers that apply
        // references only to read the result and restore the state afterwards (#5736).
        // onlyRows: when given, only the rows with the same owner and text are applied (still in dependency order); the tool uses it
        // to re-read just the rows a restored undo snapshot affects.
        public static void ApplyVariableReferences(this ElementSave element, StateSave stateSave, GraphicalUiElement? liveRoot = null,
            bool isFullCommit = true, bool notifyChanges = true, ICollection<ReferenceRow>? onlyRows = null)
        {
            // Rows run in dependency order, so a row that reads another row's result sees it whatever
            // order they are written in. Rows in a cycle run last, in written order.
            VariableReferenceGraph graph = VariableReferenceGraph.GetForState(element, stateSave);
#if FULL_DIAGNOSTICS
            ReportCyclicRows(element, graph);
#endif
            foreach (ReferenceRow row in graph.LoadOrderRows)
            {
                // Matched by text because a graph built per state creates its own row objects.
                if (onlyRows != null && !onlyRows.Any(item => item.Line == row.Line && item.SourceObject == row.SourceObject))
                {
                    continue;
                }
                // this applies the variable and returns info about the application:
                var result = ApplyVariableReferencesOnSpecificOwner(row.Instance, row.Line, stateSave, liveRoot);
                // In the gum tool, we need to check if the applicatoin actually changed the value
                // If so, we notify plugins that the variable was changed in case any additional changes
                // need to happen
                if (!string.IsNullOrEmpty(result.VariableName))
                {
                    // IsNullOrEmpty above rules out null.
                    var unqualified = result.VariableName!;
                    if (unqualified.Contains(".") == true)
                    {
                        unqualified = unqualified.Substring(unqualified.IndexOf(".") + 1);
                    }
                    if (notifyChanges && !ValueEquality(result.OldValue, result.NewValue))
                    {
                        VariableChangedThroughReference?.Invoke(
                            element, null, unqualified, result.OldValue, isFullCommit);
                    }
                }
            }
        }

        /// <summary>
        /// Loops through all variable references in the argument graphicalUiElement, evaluates them, then applies the evlauated value to the
        /// owner of each variable reference.
        /// </summary>
        /// <param name="graphicalElement">The top level owner for which to apply variables.</param>
        /// <param name="stateSave">The current state, such as the argument graphicalElement's default state</param>
        public static void ApplyVariableReferences(this GraphicalUiElement graphicalElement, StateSave stateSave)
        {
            HashSet<string>? cyclicRows = GetCyclicRowKeys(stateSave);
            foreach (var variableList in stateSave.VariableLists)
            {
                if (variableList.GetRootName() == "VariableReferences" && variableList.ValueAsIList.Count > 0)
                {
                    if (variableList.SourceObject == null)
                    {
                        ElementSave? channelOwner = stateSave.ParentContainer;
                        foreach (string referenceString in variableList.ValueAsIList)
                        {
                            foreach (string expandedReferenceString in ExpandCompositeReferenceLine(referenceString, channelOwner))
                            {
                                if (cyclicRows?.Contains(CyclicRowKey(null, expandedReferenceString)) == true)
                                {
                                    continue;
                                }
                                ApplyVariableReferencesOnSpecificOwner(graphicalElement, expandedReferenceString, stateSave, graphicalElement);
                            }
                        }
                    }
                    else
                    {
                        GraphicalUiElement? instance = null;

                        if (graphicalElement.Tag is InstanceSave asInstanceSave && asInstanceSave.Name == variableList.SourceObject)
                        {
                            instance = graphicalElement;
                        }
                        else
                        {
                            // Give preferential treatment to the children of graphicalElement. If none are found, then go to the managers
                            //
                            instance = graphicalElement.GetGraphicalUiElementByName(variableList.SourceObject);
                        }

                        if (instance != null)
                        {
                            ElementSave? channelOwner = instance.Tag is InstanceSave instanceSave
                                ? ObjectFinder.Self.GetElementSave(instanceSave)
                                : null;
                            foreach (string referenceString in variableList.ValueAsIList)
                            {
                                foreach (string expandedReferenceString in ExpandCompositeReferenceLine(referenceString, channelOwner))
                                {
                                    if (cyclicRows?.Contains(CyclicRowKey(variableList.SourceObject, expandedReferenceString)) == true)
                                    {
                                        continue;
                                    }
                                    ApplyVariableReferencesOnSpecificOwner(instance, expandedReferenceString, stateSave, graphicalElement);
                                }
                            }
                        }
                    }
                }
            }
        }

        // Rows that read each other keep their stored values (#6871); null when the state has no loop.
        private static HashSet<string>? GetCyclicRowKeys(StateSave stateSave)
        {
            ElementSave? element = stateSave.ParentContainer;
            if (element == null)
            {
                return null;
            }
            VariableReferenceGraph graph = VariableReferenceGraph.GetForApply(element, stateSave);
            if (graph.CyclicRows.Count == 0)
            {
                return null;
            }
#if FULL_DIAGNOSTICS
            ReportCyclicRows(element, graph);
#endif
            HashSet<string> keys = new HashSet<string>();
            foreach (ReferenceRow row in graph.CyclicRows)
            {
                keys.Add(CyclicRowKey(row.SourceObject, row.Line));
            }
            return keys;
        }

        private static string CyclicRowKey(string? sourceObject, string line) => sourceObject + "|" + line;

#if FULL_DIAGNOSTICS
        // Rows in a loop are skipped (#6871), so a game would otherwise never learn of the loop. An element reports it once,
        // however often its references are applied, so a reload of the project (new elements) reports again.
        private static readonly ConditionalWeakTable<ElementSave, object> ReportedCycles = new ConditionalWeakTable<ElementSave, object>();

        private static void ReportCyclicRows(ElementSave element, VariableReferenceGraph graph)
        {
            IReadOnlyList<ReferenceRow> cyclic = graph.CyclicRows;
            if (cyclic.Count == 0)
            {
                return;
            }

            lock (ReportedCycles)
            {
                if (ReportedCycles.TryGetValue(element, out _))
                {
                    return;
                }
                ReportedCycles.Add(element, new object());
            }

            string[] rows = new string[cyclic.Count];
            for (int i = 0; i < rows.Length; i++)
            {
                ReferenceRow row = cyclic[i];
                rows[i] = string.IsNullOrEmpty(row.SourceObject) ? row.Line : row.SourceObject + "." + row.Line;
            }
            RaiseReferenceError($"Circular variable reference in {element.Name}: {string.Join("; ", rows)}. " +
                "These rows are not evaluated and keep their stored values; remove one of them to break the loop.");
        }
#endif

        static char[] equalsArray = new char[] { '=' };

        /// <summary>
        /// A composite reference property (e.g. <c>Color</c>) backed by several sibling "channel"
        /// variables (e.g. Red/Green/Blue) that must stay in sync when the composite is referenced
        /// as a whole (<c>Color = Other.Color</c>). Mirrors the tool-side
        /// <c>CompositeMemberDescriptor</c> used for the Variables-tab swatch, minus the WPF-only
        /// compose/decompose/display concerns - this half only needs to expand a reference line
        /// into one line per channel at apply time.
        /// </summary>
        private readonly struct CompositeReferenceDescriptor
        {
            public string CompositeToken { get; }
            public string[] ChannelRootNames { get; }

            public CompositeReferenceDescriptor(string compositeToken, string[] channelRootNames)
            {
                CompositeToken = compositeToken;
                ChannelRootNames = channelRootNames;
            }

            /// <summary>
            /// If <paramref name="compositeName"/> contains this descriptor's token surrounded by
            /// some prefix/suffix (e.g. <c>StrokeColor</c>, <c>Color2</c>), outputs the matching
            /// channel variable names (e.g. <c>StrokeColor</c> -&gt; StrokeRed/StrokeGreen/StrokeBlue).
            /// </summary>
            public bool TryGetChannelNames(string compositeName, out IReadOnlyList<string> channelNames)
            {
                int index = compositeName.IndexOf(CompositeToken, StringComparison.Ordinal);
                if (index < 0)
                {
                    channelNames = Array.Empty<string>();
                    return false;
                }

                string prefix = compositeName.Substring(0, index);
                string suffix = compositeName.Substring(index + CompositeToken.Length);

                channelNames = ChannelRootNames.Select(token => prefix + token + suffix).ToArray();
                return true;
            }
        }

        private static readonly CompositeReferenceDescriptor[] CompositeReferenceDescriptors = new[]
        {
            new CompositeReferenceDescriptor("Color", new[] { "Red", "Green", "Blue" }),
            new CompositeReferenceDescriptor("CornerRadius", new[]
            {
                "CornerRadius", "CustomRadiusTopLeft", "CustomRadiusTopRight",
                "CustomRadiusBottomLeft", "CustomRadiusBottomRight"
            }),
        };

        /// <summary>
        /// Returns the channel root names for a known composite token (e.g. <c>"Color"</c> -&gt;
        /// Red/Green/Blue), or null if the token isn't registered. Exposed so other
        /// composite-aware consumers - e.g. the tool's <c>CompositeMemberRegistry</c>, which adds
        /// WPF display/compose concerns on top - can source the channel list from one place
        /// instead of duplicating it.
        /// </summary>
        public static string[]? GetCompositeChannelRootNames(string compositeToken)
        {
            foreach (CompositeReferenceDescriptor descriptor in CompositeReferenceDescriptors)
            {
                if (descriptor.CompositeToken == compositeToken)
                {
                    return descriptor.ChannelRootNames;
                }
            }

            return null;
        }

        /// <summary>
        /// True only if <paramref name="channelOwner"/> declares a root variable for every channel -
        /// i.e. the composite name really is backed by real channels on this element, not just a
        /// coincidental name match (e.g. a literal "BackgroundColor" scalar with no
        /// BackgroundRed/Green/Blue).
        /// </summary>
        private static bool OwnerHasAllCompositeChannels(IReadOnlyList<string> channelNames, ElementSave? channelOwner)
        {
            if (channelOwner == null)
            {
                return false;
            }

            foreach (string channelName in channelNames)
            {
                if (ObjectFinder.Self.GetRootVariable(channelName, channelOwner) == null)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Expands a single reference line into one line per channel when its left side is a
        /// composite property whose right side ends in another composite name of the *same*
        /// underlying token (e.g. <c>Color = Other.Color</c>, or a cross-named
        /// <c>DropshadowColor = Other.FillColor</c>). Left and right channel names are paired
        /// positionally (channel order is fixed per descriptor - e.g. Red/Green/Blue), so the two
        /// sides don't need identical composite names, only the same composite token (both
        /// "Color", not one "Color" and one "CornerRadius"). Returns the line unchanged (as a
        /// single-item sequence) when it isn't a composite reference. This is what lets the
        /// collapsed form stay on disk while every apply/evaluate/validate path downstream only
        /// ever sees plain single-channel lines. Public so other GumCommon-tier consumers of raw
        /// <c>VariableReferences</c> line text (e.g. <c>HeadlessErrorChecker</c>'s reference-conflict
        /// check) can expand consistently with how references are actually applied, instead of
        /// duplicating the channel table.
        /// </summary>
        public static IEnumerable<string> ExpandCompositeReferenceLine(string referenceString, ElementSave? channelOwner)
        {
            if (string.IsNullOrEmpty(referenceString) || referenceString.StartsWith("//"))
            {
                yield return referenceString;
                yield break;
            }

            string[] split = referenceString
                .Split(equalsArray, 2, StringSplitOptions.RemoveEmptyEntries)
                .Select(item => item.Trim())
                .ToArray();

            if (split.Length == 2)
            {
                string left = split[0];
                string right = split[1];
                int lastDot = right.LastIndexOf('.');

                if (lastDot >= 0)
                {
                    string rightMemberName = right.Substring(lastDot + 1);
                    string withoutVariable = right.Substring(0, lastDot);

                    foreach (CompositeReferenceDescriptor descriptor in CompositeReferenceDescriptors)
                    {
                        if (descriptor.TryGetChannelNames(left, out IReadOnlyList<string> leftChannelNames) &&
                            descriptor.TryGetChannelNames(rightMemberName, out IReadOnlyList<string> rightChannelNames) &&
                            OwnerHasAllCompositeChannels(leftChannelNames, channelOwner))
                        {
                            for (int i = 0; i < leftChannelNames.Count; i++)
                            {
                                yield return $"{leftChannelNames[i]} = {withoutVariable}.{rightChannelNames[i]}";
                            }
                            yield break;
                        }
                    }
                }
            }

            yield return referenceString;
        }

        /// <summary>
        /// Evaluates the reference string (such as X = SomeOtherItem.X), applying the right side to the left side.
        /// </summary>
        /// <param name="referenceOwner">The owner that owns the variable reference, such as the instance.</param>
        /// <param name="referenceString">The string such as "X = SomeItem.X"</param>
        /// <param name="stateSave">The state save which owns the variable reference.</param>
        /// <param name="liveRootForRightSide">The top-level live <see cref="GraphicalUiElement"/> to
        /// search within for a right-side runtime-computed identifier such as <c>AbsoluteWidth</c>
        /// (e.g. "Source.AbsoluteWidth"). Defaults to <paramref name="referenceOwner"/> when not
        /// given, since instance names are scoped to the owning element, not to the instance that
        /// happens to own the reference.</param>
        public static void ApplyVariableReferencesOnSpecificOwner(GraphicalUiElement referenceOwner, string referenceString, StateSave stateSave, GraphicalUiElement? liveRootForRightSide = null)
        {
            if (!TryReadReference(referenceOwner.Tag as InstanceSave, referenceString, stateSave, liveRootForRightSide ?? referenceOwner,
                out string left, out _, out _, out object? value, out _))
            {
                return;
            }

            if (value != null)
            {
                referenceOwner.SetProperty(left, value);
            }
        }

        /// <summary>
        /// Rewrites the <c>@</c> owner prefix in a reference expression. <c>@Index</c> means "the instance
        /// that owns this row", so on an instance row it becomes <c>Owner.Index</c>; on an element-level row
        /// (<paramref name="ownerInstanceName"/> null) the owner is the element itself, so the <c>@</c> is
        /// dropped. String and char literals are left untouched, as is a verbatim string opener (<c>@"</c>).
        /// </summary>
        /// <param name="expression">The reference expression, such as <c>@Index * 2</c>.</param>
        /// <param name="ownerInstanceName">The instance that owns the row; null on an element-level row.</param>
        /// <param name="instanceNames">Names of the element's instances. Any that is not a valid C#
        /// identifier (such as <c>gum-logo-256</c>) is encoded wherever it appears at a token boundary
        /// followed by <c>.</c>, so the expression parses as a member access instead of a subtraction.
        /// The longest matching name wins, so <c>a-b.Width</c> reads as instance <c>a-b</c> even when an
        /// instance <c>a</c> exists.</param>
        /// <param name="resolveOwner">When false the <c>@</c> prefix is left as written and only
        /// <paramref name="instanceNames"/> are encoded.</param>
        public static string ResolveOwnerPrefix(string expression, string? ownerInstanceName, IEnumerable<string>? instanceNames = null, bool resolveOwner = true)
        {
            List<string>? namesToEncode = instanceNames?
                .Where(name => name.Length > 0 && EncodeOwnerName(name) != name)
                .Distinct()
                .OrderByDescending(name => name.Length)
                .ToList();
            bool hasNamesToEncode = namesToEncode != null && namesToEncode.Count > 0;

            if ((!resolveOwner || expression.IndexOf('@') < 0) && !hasNamesToEncode)
            {
                return expression;
            }

            var builder = new System.Text.StringBuilder(expression.Length + 16);

            for (int i = 0; i < expression.Length; i++)
            {
                char c = expression[i];

                if (c == '"' || c == '\'')
                {
                    int end = i + 1;
                    while (end < expression.Length && expression[end] != c)
                    {
                        end += expression[end] == '\\' ? 2 : 1;
                    }
                    end = System.Math.Min(end + 1, expression.Length);
                    builder.Append(expression, i, end - i);
                    i = end - 1;
                }
                else if (resolveOwner && c == '@' && i + 1 < expression.Length && (char.IsLetter(expression[i + 1]) || expression[i + 1] == '_'))
                {
                    if (ownerInstanceName != null)
                    {
                        builder.Append(EncodeOwnerName(ownerInstanceName)).Append('.');
                    }
                }
                else if (hasNamesToEncode && TryMatchInstanceName(expression, i, namesToEncode!, out string? matchedName))
                {
                    builder.Append(EncodeOwnerName(matchedName!));
                    i += matchedName!.Length - 1;
                }
                else
                {
                    builder.Append(c);
                }
            }

            return builder.ToString();
        }

        private static bool TryMatchInstanceName(string expression, int index, List<string> namesLongestFirst, out string? matchedName)
        {
            matchedName = null;

            if (index > 0)
            {
                char previous = expression[index - 1];
                if (char.IsLetterOrDigit(previous) || previous == '_' || previous == '.' || previous == ':' || previous == '/')
                {
                    return false;
                }
            }

            foreach (string name in namesLongestFirst)
            {
                int end = index + name.Length;
                if (end < expression.Length
                    && expression[end] == '.'
                    && string.CompareOrdinal(expression, index, name, 0, name.Length) == 0)
                {
                    matchedName = name;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// The names of every instance <paramref name="element"/> can reference: its own and those of its
        /// base elements. Pass to <see cref="ResolveOwnerPrefix"/> so names that are not valid identifiers
        /// still parse.
        /// </summary>
        public static IEnumerable<string> GetReferencableInstanceNames(ElementSave? element)
        {
            if (element == null)
            {
                return Enumerable.Empty<string>();
            }

            return ObjectFinder.Self.GetBaseElements(element)
                .Append(element)
                .SelectMany(item => item.Instances)
                .Select(instance => instance.Name);
        }

        private static readonly System.Text.RegularExpressions.Regex EncodedOwnerCharRegex =
            new System.Text.RegularExpressions.Regex("_x([0-9A-F]{4})_", System.Text.RegularExpressions.RegexOptions.Compiled);

        // The rewritten expression is parsed as C#, so an owner name such as "gum-logo-256" would read as
        // a subtraction. Characters that cannot appear in an identifier are written as _xHHHH_.
        private static string EncodeOwnerName(string name)
        {
            var builder = new System.Text.StringBuilder(name.Length + 8);
            for (int i = 0; i < name.Length; i++)
            {
                char c = name[i];
                bool isIdentifierChar = c == '_' || (c < 128 && (char.IsLetter(c) || (i > 0 && char.IsDigit(c))));
                if (isIdentifierChar)
                {
                    builder.Append(c);
                }
                else
                {
                    builder.Append("_x").Append(((int)c).ToString("X4")).Append('_');
                }
            }
            return builder.ToString();
        }

        /// <summary>
        /// Undoes the encoding <see cref="ResolveOwnerPrefix"/> applies to an owner name that is not a valid
        /// identifier, so a path read out of the parsed expression names the real instance again.
        /// </summary>
        public static string DecodeOwnerName(string text)
        {
            return text.IndexOf("_x", System.StringComparison.Ordinal) < 0
                ? text
                : EncodedOwnerCharRegex.Replace(text, match => ((char)System.Convert.ToInt32(match.Groups[1].Value, 16)).ToString());
        }

        private static string? GetRootVariableType(string leftVariableName, InstanceSave? instanceLeft, StateSave stateSave)
        {
            return GetRootVariable(leftVariableName, instanceLeft, stateSave)?.Type;
        }


#nullable enable
        private static VariableSave? GetRootVariable(string leftVariableName, InstanceSave? instanceLeft, StateSave stateSave)
        {
            // Variable references need a state owned by an element (RecursiveVariableFinder requires it).
            var elementOwningInstance = instanceLeft?.ParentContainer ?? stateSave.ParentContainer!;
            return ObjectFinder.Self.GetRootVariable(leftVariableName, elementOwningInstance);
        }

        // The type of the variable a reference row writes: what the state says, else the element's root definition.
        private static string? GetLeftSideType(VariableSave? leftVariableOnState, string leftVariableName, InstanceSave? instanceLeft, StateSave stateSave)
        {
            return leftVariableOnState?.Type ?? GetRootVariableType(leftVariableName, instanceLeft, stateSave);
        }
#nullable restore

        public struct VariableReferenceAssignmentResult
        {
            public string? VariableName;
            public object? OldValue;
            public object? NewValue;

            /// <summary>
            /// True when <see cref="NewValue"/> is null because the reference evaluated to null for a nullable
            /// variable and null was written (see <c>propagateNull</c>), not because nothing was evaluated.
            /// </summary>
            public bool AssignsNull;
        }

        /// <summary>
        /// Evaluates a single reference line (e.g. <c>X = SomeOther.X</c>) against
        /// <paramref name="stateSave"/>, qualifying the left side with
        /// <paramref name="instanceLeft"/>'s name when present, and writes the result
        /// back into the state. Used by both state-level <c>VariableReferences</c>
        /// application and (tool-only) behavior <c>ToolOnlyVariableReferences</c>
        /// application. Returns the resulting variable name + old/new values so callers
        /// can fire change notifications. <paramref name="liveRoot"/> is the live <see
        /// cref="GraphicalUiElement"/> to search within for a right-side runtime-computed identifier
        /// such as <c>AbsoluteWidth</c>, when one is available (e.g. the tool's currently-rendered
        /// wireframe for this element); null when applying against pure save data with nothing
        /// rendered, in which case such identifiers are simply unresolvable.
        /// </summary>
        public static VariableReferenceAssignmentResult ApplyVariableReferencesOnSpecificOwner(InstanceSave? instanceLeft, string referenceString, StateSave stateSave, GraphicalUiElement? liveRoot = null)
        {
            return ApplyVariableReferencesOnSpecificOwner(instanceLeft, referenceString, stateSave, liveRoot, propagateNull: false);
        }

        // propagateNull: when true, a null result is written to a nullable variable instead of being skipped.
#nullable enable
        internal static VariableReferenceAssignmentResult ApplyVariableReferencesOnSpecificOwner(InstanceSave? instanceLeft, string referenceString, StateSave stateSave, GraphicalUiElement? liveRoot, bool propagateNull)
        {

            if (!TryReadReference(instanceLeft, referenceString, stateSave, liveRoot,
                out string left, out string right, out string? leftSideType, out object? value, out bool mismatchReported))
            {
                return new VariableReferenceAssignmentResult { NewValue = null, OldValue = null, VariableName = null };
            }

            object? valueBefore = null;
            string? effectiveLeft = null;
            // A null result is a value only for a nullable variable, only when the caller asks (live re-evaluation),
            // and only when the right side is a plain reference to a variable that exists and holds null
            // (MinWidth = Other.MinWidth). Any other null is a failed evaluation, which leaves the variable alone.
            bool assignsNull = value == null && !mismatchReported && propagateNull && leftSideType != null && leftSideType.EndsWith("?")
                && IsVariableOfCompatibleType(stateSave, right, leftSideType!);
            if (value != null || assignsNull)
            {
                if (instanceLeft == null)
                {
                    effectiveLeft = left;
                }
                else
                {
                    effectiveLeft = $"{instanceLeft.Name}.{left}";
                }
                valueBefore = stateSave.GetValue(effectiveLeft);
                stateSave.SetValue(effectiveLeft, value, instanceLeft, assignsNull ? leftSideType : null);
            }

            return new VariableReferenceAssignmentResult
            {
                NewValue = value,
                OldValue = valueBefore,
                VariableName = effectiveLeft,
                AssignsNull = assignsNull
            };
        }

        // True when the text is a plain dotted variable name (no operators, no cross-element path) that exists and
        // whose type can be the target's: the same type, or int/float/double, ignoring nullability. A variable of a
        // mismatched type (a bool read into a float?) fails the cast, which is not a null to copy.
        private static bool IsVariableOfCompatibleType(StateSave stateSave, string right, string targetType)
        {
            string name = DecodeOwnerName(right).Trim();
            if (name.Length == 0)
            {
                return false;
            }
            foreach (char c in name)
            {
                if (!char.IsLetterOrDigit(c) && c != '_' && c != '.')
                {
                    return false;
                }
            }
            VariableSave? variable = new RecursiveVariableFinder(stateSave).GetVariable(name);
            if (variable == null)
            {
                return false;
            }
            string? sourceType = GetRootVariableType(name, null, stateSave) ?? variable.Type;
            if (sourceType == null)
            {
                return false;
            }
            sourceType = sourceType.TrimEnd('?');
            targetType = targetType.TrimEnd('?');
            return sourceType == targetType || (IsNumericTypeName(sourceType) && IsNumericTypeName(targetType));
        }

        private static bool IsNumericTypeName(string type) => type == "int" || type == "float" || type == "double";

        // The part of applying a reference line that both overloads of ApplyVariableReferencesOnSpecificOwner share:
        // split the line, find the left side's type and read the right side. Returns false for a comment or a line
        // that is not "left = right". A value that does not fit the left variable (see ReportedMismatch) comes back
        // null with mismatchReported set, so a caller does not treat that null as a value.
        private static bool TryReadReference(InstanceSave? instanceLeft, string referenceString, StateSave stateSave,
            GraphicalUiElement? liveRoot, out string left, out string right, out string? leftSideType, out object? value,
            out bool mismatchReported)
        {
            left = string.Empty;
            right = string.Empty;
            leftSideType = null;
            value = null;
            mismatchReported = false;

            if (referenceString.StartsWith("//"))
            {
                return false;
            }

            // splits the left and right side, so that we get two items. the first is the left side like "X" and the second is the right side like "SomeItem.X"
            string[] split = referenceString
                .Split(equalsArray, 2, StringSplitOptions.RemoveEmptyEntries)
                .Select(item => item.Trim()).ToArray();

            if (split.Length != 2)
            {
                return false;
            }

            left = split[0];

            string leftVariableName = instanceLeft == null
                ? left
                : instanceLeft.Name + "." + left;

            VariableSave? leftVariableOnState = stateSave.Variables.FirstOrDefault(item => item.Name == leftVariableName);
            leftSideType = GetLeftSideType(leftVariableOnState, leftVariableName, instanceLeft, stateSave);

            right = ResolveOwnerPrefix(split[1], instanceLeft?.Name, GetReferencableInstanceNames(stateSave.ParentContainer));

            // Without Gum.Expressions the plain lookup also says what type the source variable is; a negation yields a bool.
            string? sourceType = null;
            if (CustomEvaluateExpression == null && !IsNegation(right))
            {
                value = ReadPlainReference(stateSave, right, findSourceType: true, out sourceType);
            }
            else
            {
                RejectedCastValue = null;
                value = GetRightSideValue(stateSave, right, leftSideType, liveRoot);
                sourceType = CustomEvaluateExpression == null ? "bool" : null;
                object? rejectedValue = RejectedCastValue;
                RejectedCastValue = null;
                if (value == null && CustomEvaluateExpression != null && rejectedValue != null && leftSideType != null
                    && ReportedRejectedCast(referenceString, right, leftVariableName, leftSideType, rejectedValue, stateSave, liveRoot))
                {
                    // Gum.Expressions evaluated the right side but refused to cast it to the left side's type.
                    mismatchReported = true;
                }
            }

            // With Gum.Expressions the evaluator converts what it can and refuses the rest (reported above).
            if (CustomEvaluateExpression == null
                && ReportedMismatch(referenceString, right, left, leftVariableName, leftSideType, sourceType, value, leftVariableOnState, instanceLeft, stateSave, liveRoot))
            {
                value = null;
                mismatchReported = true;
            }

            // A float or double variable never takes a NaN or infinity from a reference (#6457). The setters of X, Y, Width,
            // Height, Rotation and FontScale throw on one under FULL_DIAGNOSTICS and take it silently otherwise, and no other
            // float has a use for it, so the one check sits here, in front of both. A nullable float is left alone: infinity
            // is a usable MaxWidth. A live update's state holds only the changed and input variables, so a variable that is
            // not on a standard element (Z, a custom variable) takes its type from the element's own declaration.
            if (value != null && !mismatchReported && IsNonFiniteNumber(value))
            {
                string? numberType = leftSideType ?? GetDeclaredType(leftVariableName, instanceLeft, stateSave);
                if (numberType != null && (numberType == "float" || numberType == "double")
                    && ReportedRejectedCast(referenceString, right, leftVariableName, numberType, value, stateSave, liveRoot))
                {
                    value = null;
                    mismatchReported = true;
                }
            }

            if (value == null && !mismatchReported
                && ReportedTrimmedSource(referenceString, leftVariableName, stateSave, liveRoot))
            {
                mismatchReported = true;
            }
            return true;
        }

        // A row reading an element that gumcli pack --shake left out of the bundle finds nothing to read. That is
        // reported once per row and owner rather than skipped silently, naming the --keep that brings it back.
        private static bool ReportedTrimmedSource(string referenceString, string leftVariableName, StateSave stateSave,
            GraphicalUiElement? liveRoot)
        {
            List<string>? trimmed = ObjectFinder.Self.GumProjectSave?.TrimmedElements;
            if (trimmed == null || trimmed.Count == 0)
            {
                return false;
            }

            foreach (string elementName in trimmed)
            {
                if (ReferenceDependencies.RowReadsElement(referenceString, ElementReference.ComponentSubfolder + "/" + elementName)
                    || ReferenceDependencies.RowReadsElement(referenceString, ElementReference.ScreenSubfolder + "/" + elementName))
                {
                    if (IsFirstReport(liveRoot, stateSave, referenceString, leftVariableName))
                    {
                        ReportNotApplied(referenceString, leftVariableName, stateSave, liveRoot,
                            $"it reads {elementName}, which gumcli pack --shake trimmed; pack with --keep {elementName}.");
                    }
                    return true;
                }
            }
            return false;
        }

        private static bool IsNonFiniteNumber(object value)
        {
            if (value is float single)
            {
                return float.IsNaN(single) || float.IsInfinity(single);
            }
            return value is double precise && (double.IsNaN(precise) || double.IsInfinity(precise));
        }

        // The type the element (or the base it derives from) declares for a variable on its default state, for a
        // variable no standard element defines. Null when nothing declares it. Plain loops, since a live row calls this.
        private static string? GetDeclaredType(string variableName, InstanceSave? instanceLeft, StateSave stateSave)
        {
            // The owner whose rows run comes first: an inherited row's instance belongs to the base, but the owner (and the
            // declarations it adds) is the derived element, and walking its bases reaches the instance's own container.
            ElementSave? element = stateSave.ParentContainer ?? instanceLeft?.ParentContainer;
            for (int depth = 0; element != null && depth < 32; depth++)
            {
                List<VariableSave>? variables = element.DefaultState?.Variables;
                if (variables != null)
                {
                    for (int i = 0; i < variables.Count; i++)
                    {
                        if (variables[i].Name == variableName && variables[i].Type != null)
                        {
                            return variables[i].Type;
                        }
                    }
                }
                element = ObjectFinder.Self.GetElementSave(element.BaseType);
            }
            return null;
        }

        // True, after reporting once per row, when a value cannot be applied to the left side's type: Gum.Expressions evaluated
        // the right side but refused to cast it (#6463: a bool or text into a number, an enum into a number, a name that is no
        // value of the enum, a float or double the target cannot hold, #6456), or the value is a NaN or infinity for a float
        // or double variable (#6457). The variable keeps its value, the same as for a mismatch without the package, so a
        // reference is never silently dropped.
        private static bool ReportedRejectedCast(string referenceString, string right, string leftVariableName,
            string leftSideType, object rejectedValue, StateSave stateSave, GraphicalUiElement? liveRoot)
        {
            if (IsFirstReport(liveRoot, stateSave, referenceString, leftVariableName))
            {
                string valueType = TypeNameOf(rejectedValue);
                string shownRight = DecodeOwnerName(right);
                string detail;
                string targetType = StripNullable(leftSideType);
                if ((rejectedValue is float || rejectedValue is double) && IsNumericTypeName(targetType))
                {
                    detail = $"{shownRight} is {Convert.ToString(rejectedValue, System.Globalization.CultureInfo.InvariantCulture)}, " +
                        $"which does not fit the {targetType} {leftVariableName}.";
                }
                else if (rejectedValue is string name && !IsPrimitiveTypeName(targetType))
                {
                    detail = $"{shownRight} is \"{name}\", which is not a value of the {targetType} {leftVariableName}.";
                }
                else
                {
                    detail = $"{shownRight} is {Article(valueType)} {valueType} and {leftVariableName} is {Article(targetType)} {targetType}, which cannot be converted.";
                }
                ReportNotApplied(referenceString, leftVariableName, stateSave, liveRoot, detail);
            }
            return true;
        }

        // The one wording of a reference that was not applied; the caller says why in detail.
        private static void ReportNotApplied(string referenceString, string leftVariableName, StateSave stateSave,
            GraphicalUiElement? liveRoot, string detail)
        {
            object? owner = liveRoot?.Tag ?? stateSave.ParentContainer;
            string where = owner == null ? string.Empty : $" in {owner}";
            RaiseReferenceError($"Variable reference '{referenceString}' was not applied{where}: {detail} {leftVariableName} keeps its current value.");
        }

        private static string TypeNameOf(object value)
        {
            return value is int ? "int"
                : value is float ? "float"
                : value is double ? "double"
                : value is string ? "string"
                : value is bool ? "bool"
                : value.GetType().Name;
        }

        // True when truncating the value gives an int without overflowing; shared with Gum.Expressions' CastTo (#6456). Both
        // bounds are exclusive (2^31 and -2^31 - 1) so a fraction such as -2147483648.5 still truncates to int.MinValue. NaN
        // fails both comparisons. A float widens to a double exactly, so one overload serves both.
        internal static bool IsInIntRange(double value) => value > -2147483649.0 && value < 2147483648.0;

        private static bool IsNegation(string right)
        {
            int index = 0;
            while (index < right.Length && char.IsWhiteSpace(right[index]))
            {
                index++;
            }
            return index < right.Length && right[index] == '!';
        }

        // Without Gum.Expressions nothing converts the right side's value, so a value of another type reaches the
        // variable's setter as is and throws (or, into a string, silently blanks it). Compares the declared types, because
        // a live read can hand a float to an int variable (FontSize) that creation would reject. A mismatch keeps the
        // variable's value and is reported once per broken row (see IsFirstReport). What the setters already take stays:
        // a FontSize takes an int or a float, and an enum takes an int.
        private static bool ReportedMismatch(string referenceString, string right, string left, string leftVariableName,
            string? leftSideType, string? sourceType, object? value, VariableSave? leftVariableOnState, InstanceSave? instanceLeft,
            StateSave stateSave, GraphicalUiElement? liveRoot)
        {
            if (value == null || leftSideType == null)
            {
                return false;
            }

            // The common case, a valid row, must not allocate: compare in place and only build strings for an error.
            string? valueType = sourceType;
            if (string.IsNullOrEmpty(valueType))
            {
                valueType = TypeNameOf(value);
            }
            if (SameBaseType(valueType, leftSideType))
            {
                return false;
            }

            string targetType = StripNullable(leftSideType);
            valueType = StripNullable(valueType);

            bool targetIsEnum = false;
            if (IsPrimitiveTypeName(targetType))
            {
                if (left == "FontSize" && (valueType == "int" || valueType == "float"))
                {
                    return false;
                }
            }
            else
            {
                // A state name or a type this does not know is left to the setter. Only an enum is judged.
                VariableSave? rootVariable = leftVariableOnState ?? GetRootVariable(leftVariableName, instanceLeft, stateSave);
                targetIsEnum = (leftVariableOnState?.Value ?? rootVariable?.Value) is Enum;
                if (!targetIsEnum || valueType == "int")
                {
                    return false;
                }
            }

            if (IsFirstReport(liveRoot, stateSave, referenceString, leftVariableName))
            {
                bool expressionsConverts = (IsNumericTypeName(valueType) && IsNumericTypeName(targetType))
                    || targetType == "string"
                    || (targetIsEnum && valueType == "string");
                string reason = expressionsConverts
                    ? ". Add the Gum.Expressions package and call GumExpressionService.Initialize() to convert between them."
                    : ", which cannot be converted.";
                ReportNotApplied(referenceString, leftVariableName, stateSave, liveRoot,
                    $"{DecodeOwnerName(right)} is {Article(valueType)} {valueType} and {leftVariableName} is {Article(targetType)} {targetType}{reason}");
            }
            return true;
        }

        private static bool SameBaseType(string first, string second)
        {
            int firstLength = first.Length > 0 && first[first.Length - 1] == '?' ? first.Length - 1 : first.Length;
            int secondLength = second.Length > 0 && second[second.Length - 1] == '?' ? second.Length - 1 : second.Length;
            return firstLength == secondLength && string.CompareOrdinal(first, 0, second, 0, firstLength) == 0;
        }

        private static string StripNullable(string type) => type.TrimEnd('?');

        private static bool IsPrimitiveTypeName(string type) =>
            type == "int" || type == "float" || type == "double" || type == "string" || type == "bool";

        private static string Article(string typeName) => "AEIOUaeiou".IndexOf(typeName[0]) >= 0 ? "an" : "a";

        // A broken row is evaluated again every time a variable it reads changes, which can be every frame. Each owner
        // (the live element, or with none the element the state belongs to) reports a given row once: a new owner reports
        // again, and ApplyAllVariableReferences starts over. The check sits here, in front of the shared error channel,
        // so other users of PropertyAssignmentError are unaffected.
        private static readonly ConditionalWeakTable<GraphicalUiElement, HashSet<(string Line, string Variable)>> ReportedByOwner =
            new ConditionalWeakTable<GraphicalUiElement, HashSet<(string Line, string Variable)>>();

        private static readonly HashSet<(string Owner, string Line, string Variable)> ReportedWithoutOwner =
            new HashSet<(string Owner, string Line, string Variable)>();

        // Set by each platform's SystemManagers to CustomSetPropertyOnRenderable.RaisePropertyAssignmentError, which
        // writes the [Gum] line to Console.Error and raises PropertyAssignmentError. GumRuntime sits below that class.
        internal static Action<string>? ReportPropertyAssignmentError;

        private static bool IsFirstReport(GraphicalUiElement? liveRoot, StateSave stateSave, string referenceString, string variableName)
        {
            if (liveRoot != null)
            {
                return IsFirstReport(liveRoot, referenceString, variableName);
            }

            lock (ReportedWithoutOwner)
            {
                return ReportedWithoutOwner.Add((stateSave.ParentContainer?.Name ?? string.Empty, referenceString, variableName));
            }
        }

        // Also used by a platform that reports a value it cannot apply on every font or layout update (Skia's .fnt
        // CustomFontFile), so an element reports a given line once.
        internal static bool IsFirstReport(GraphicalUiElement owner, string line, string variableName)
        {
            HashSet<(string Line, string Variable)> reported = ReportedByOwner.GetOrCreateValue(owner);
            lock (reported)
            {
                return reported.Add((line, variableName));
            }
        }

        private static void RaiseReferenceError(string message)
        {
            try
            {
                if (ReportPropertyAssignmentError != null)
                {
                    ReportPropertyAssignmentError(message);
                }
                else
                {
                    Console.Error.WriteLine($"[Gum] {message}");
                }
            }
            catch (Exception exception)
            {
                // A subscriber that throws must not break the screen being created.
                Console.Error.WriteLine($"[Gum] A PropertyAssignmentError subscriber threw while reporting a variable reference: {exception}");
            }
        }
#nullable restore

        private static object? GetRightSideValue(StateSave stateSave, string right, string? leftSideType, GraphicalUiElement? liveRoot = null)
        {
            if (right.TrimStart().StartsWith("!"))
            {
                var withoutNot = right.TrimStart().Substring(1).Trim();
                var originalValue = GetRightSideValue(stateSave, withoutNot, leftSideType, liveRoot);
                if (originalValue is bool boolValue) return !boolValue;
                return null;
            }

            if(CustomEvaluateExpression != null)
            {
                return CustomEvaluateExpression(stateSave, right, leftSideType, liveRoot);
            }
            else
            {
                // fallback (temporarily?)
                return ReadPlainReference(stateSave, right, findSourceType: false, out _);
            }
        }

        // The lookup used without Gum.Expressions: the right side is a dotted variable name. sourceType, when asked
        // for, is that variable's declared type (null when it cannot be found).
#nullable enable
        private static object? ReadPlainReference(StateSave stateSave, string right, bool findSourceType, out string? sourceType)
        {
            sourceType = null;

            // assume the owner of the right-side is the StateSave that was passed in...
            var ownerOfRightSideVariable = stateSave;
            right = DecodeOwnerName(right);
            // ...but call this to change that in case the right-side is a variable belonging to some other component
            GetRightSideAndState(ref right, ref ownerOfRightSideVariable);

            // A read of the element's own variables through its qualified name still reads the state
            // being evaluated, which may hold values newer than the saved ones (live re-evaluation).
            if (ownerOfRightSideVariable != stateSave
                && ownerOfRightSideVariable.ParentContainer == stateSave.ParentContainer)
            {
                ownerOfRightSideVariable = stateSave;
            }

            var recursiveVariableFinder = new RecursiveVariableFinder(ownerOfRightSideVariable);

            var value = recursiveVariableFinder.GetValue(right);
            if (findSourceType && value != null)
            {
                sourceType = FindSourceType(ownerOfRightSideVariable, right, recursiveVariableFinder);
            }
            return value;
        }

        // The declared type of the variable a reference reads. Live re-evaluation seeds its state with untyped
        // values, so the type comes from the element's own definition. That lookup allocates, and a live update runs
        // it for every row every time a variable changes, so each element remembers what it found.
        private static readonly ConditionalWeakTable<ElementSave, Dictionary<string, string?>> SourceTypesByElement =
            new ConditionalWeakTable<ElementSave, Dictionary<string, string?>>();

        private static string? FindSourceType(StateSave owner, string variableName, RecursiveVariableFinder finder)
        {
            ElementSave? element = owner.ParentContainer;
            if (element == null)
            {
                string? authoredType = finder.GetVariable(variableName)?.Type;
                return string.IsNullOrEmpty(authoredType) ? null : authoredType;
            }

            Dictionary<string, string?> cache = SourceTypesByElement.GetOrCreateValue(element);
            lock (cache)
            {
                if (cache.TryGetValue(variableName, out string? cached))
                {
                    return cached;
                }
            }

            string? type = ObjectFinder.Self.GetRootVariable(variableName, element)?.Type;
            if (string.IsNullOrEmpty(type))
            {
                type = finder.GetVariable(variableName)?.Type;
            }
            if (string.IsNullOrEmpty(type))
            {
                type = null;
            }
            lock (cache)
            {
                cache[variableName] = type;
            }
            return type;
        }
#nullable restore

        /// <summary>
        /// Read-only counterpart to <see cref="GetRightSideValue"/>: enumerates every value the
        /// right side could resolve to (all ternary branches) instead of applying just the one
        /// selected by the condition's current value. Falls back to a single-item sequence
        /// wrapping <see cref="GetRightSideValue"/> when <see cref="CustomEvaluateExpressionAllBranches"/>
        /// is unset (no Roslyn evaluator wired up).
        /// </summary>
        private static IEnumerable<object> GetAllRightSideValues(StateSave stateSave, string right, string? leftSideType, GraphicalUiElement? liveRoot = null)
        {
            if (right.TrimStart().StartsWith("!"))
            {
                var withoutNot = right.TrimStart().Substring(1).Trim();
                foreach (var value in GetAllRightSideValues(stateSave, withoutNot, leftSideType, liveRoot))
                {
                    if (value is bool boolValue)
                    {
                        yield return !boolValue;
                    }
                }
                yield break;
            }

            if (CustomEvaluateExpressionAllBranches != null)
            {
                foreach (var value in CustomEvaluateExpressionAllBranches(stateSave, right, leftSideType, liveRoot))
                {
                    yield return value;
                }
            }
            else
            {
                var value = GetRightSideValue(stateSave, right, leftSideType, liveRoot);
                if (value != null)
                {
                    yield return value;
                }
            }
        }

        /// <summary>
        /// Parses a single <c>VariableReferences</c> row (e.g.
        /// <c>Font = IsLocaleZh ? "NotoSansCJK" : "Arial"</c>) and returns the qualified left-hand
        /// variable name plus every value its right side could resolve to (all ternary branches).
        /// Read-only counterpart to <see cref="ApplyVariableReferencesOnSpecificOwner(InstanceSave, string, StateSave, GraphicalUiElement)"/>
        /// for callers that need every possible value rather than applying just one - namely font
        /// bulk-generation (see issue #4042). Returns null for a comment row or a malformed
        /// (non "Left = Right") reference string.
        /// </summary>
        public static (string VariableName, IEnumerable<object> Values)? GetAllVariableReferenceBranches(InstanceSave? instanceLeft, string referenceString, StateSave stateSave, GraphicalUiElement? liveRoot = null)
        {
            if (referenceString.StartsWith("//"))
            {
                return null;
            }

            var split = referenceString
                .Split(equalsArray, 2, StringSplitOptions.RemoveEmptyEntries)
                .Select(item => item.Trim()).ToArray();

            if (split.Length != 2)
            {
                return null;
            }

            var left = split[0];
            var leftVariableName = instanceLeft == null ? left : instanceLeft.Name + "." + left;

            var variableOnState = stateSave.Variables.FirstOrDefault(item => item.Name == leftVariableName);
            string? leftSideType = GetLeftSideType(variableOnState, leftVariableName, instanceLeft, stateSave);

            var right = ResolveOwnerPrefix(split[1], instanceLeft?.Name, GetReferencableInstanceNames(stateSave.ParentContainer));
            var values =GetAllRightSideValues(stateSave, right, leftSideType, liveRoot).ToList();

            return (leftVariableName, values);
        }

        public static void GetRightSideAndState(ref string right, ref StateSave stateSave)
        {
            var isExternalElement = right.Contains("/");

            if (isExternalElement)
            {
                var lastDot = right.LastIndexOf('.');
                var firstDot = right.IndexOf('.');

                var elementNameToFind = right.Substring(0, firstDot);

                ElementSave? element = null;
                if (TryStripPrefix(elementNameToFind, ElementReference.ComponentSubfolder, out var componentName))
                {
                    element = ObjectFinder.Self.GetComponent(componentName);
                }
                else if (TryStripPrefix(elementNameToFind, ElementReference.ScreenSubfolder, out var screenName))
                {
                    element = ObjectFinder.Self.GetScreen(screenName);
                }
                else if (TryStripPrefix(elementNameToFind, ElementReference.StandardSubfolder, out var standardName))
                {
                    element = ObjectFinder.Self.GetStandardElement(standardName);
                }

                if (element != null)
                {
                    stateSave = GetRightSide(ref right, firstDot, element);
                }
            }
        }

        private static bool TryStripPrefix(string qualifiedElementName, string subfolder, out string elementName)
        {
            var prefix = subfolder + "/";
            if (qualifiedElementName.StartsWith(prefix))
            {
                elementName = qualifiedElementName.Substring(prefix.Length);
                return true;
            }
            elementName = string.Empty;
            return false;
        }

        private static StateSave GetRightSide(ref string right, int firstDot, ElementSave element)
        {
            StateSave stateSave = element.DefaultState!;
            right = right.Substring(firstDot + 1);

            if (right.Contains("."))
            {
                var dotAfterInstance = right.IndexOf(".");
                var instanceName = right.Substring(0, dotAfterInstance);
                var instance = element.GetInstance(instanceName);

                if(instance != null)
                {
                    // we found the variable on the instance
                    // do nothing to right, it's got what we need there already
                }
                else
                {
                    GetRightSideAndState(ref right, ref stateSave);
                }
            }

            return stateSave;
        }

        public static void SetGraphicalUiElement(this ElementSave elementSave, GraphicalUiElement toReturn, ISystemManagers systemManagers)
        {
#if FULL_DIAGNOSTICS
            if (elementSave == null)
            {
                throw new ArgumentNullException(nameof(elementSave), "elementSave parameter is required");
            }
            if(elementSave.DefaultState == null)
            {
                throw new ArgumentException($"The element {elementSave} must have a DefaultState set.");
            }
#endif
            // Issue #4665: this method recurses into itself for every child instance (via
            // CreateChildrenRecursively below), and building a tree of any size sets every
            // variable on every element - including font properties, and anything a stacking/
            // auto-sized parent must re-layout for - one at a time, fully unsuspended. Wrap the
            // whole build in the same suspend/resume WireframeObjectManager.RefreshAll already
            // uses when the tool builds a GraphicalUiElement tree, so a whole tree's worth of
            // font realization and layout recalculation batches into a single pass instead of
            // once per property/child. Only the outermost call (not already suspended) toggles
            // the flag and flushes, matching ApplyState's own reentrancy-safety.
            bool wasSuspended = GraphicalUiElement.IsAllLayoutSuspended;
            if (!wasSuspended)
            {
                GraphicalUiElement.IsAllLayoutSuspended = true;
            }

            // The flag is thread-wide, so it must be cleared even when the build throws (a component
            // constructor, a custom variable setter). Otherwise every later build on this thread
            // skips its own layout flush and no layout is applied again (#6066).
            try
            {
                // We need to set categories and states first since those are used below;
                toReturn.AddStatesAndCategoriesRecursivelyToGue(elementSave);

                if (toReturn.RenderableComponent == null)
                {
                    // This could have already been created by the type that is instantiated, so don't do this to double-create
                    toReturn.CreateGraphicalComponent(elementSave, systemManagers);
                }

                toReturn.AddExposedVariablesRecursively(elementSave);

                toReturn.CreateChildrenRecursively(elementSave, systemManagers);


                toReturn.Tag = elementSave;
                toReturn.ElementSave = elementSave;

                toReturn.SetInitialState();

    #if !FRB
                if (ObjectFinder.Self.GumProjectSave?.ElementAnimations.Count > 0)
                {
                    var elementAnimationsSave = ObjectFinder.Self.GumProjectSave.ElementAnimations.FirstOrDefault(item =>
                        item.ElementName == elementSave.Name);
                    if (elementAnimationsSave != null)
                    {
                        var animationRuntime = elementAnimationsSave.ToRuntime();
                        toReturn.Animations = animationRuntime;
                    }
                }
    #endif


                    toReturn.AfterFullCreation();

                // Forms controls (e.g. RadioButton, CheckBox) apply their default IsChecked visual
                // inside AfterFullCreation, but a parent's SetInitialState can override that category
                // state afterward. Notify the root and walk its subtree so Forms controls can
                // re-assert their state. The root is included so a Forms-behavior component built
                // directly (not as an instance in a parent) still gets its own FormsProperty
                // defaults applied.
                InitialStateAppliedNotifier?.Invoke(toReturn);
                NotifyFormsControlsOfInitialStateApplied(toReturn);
            }
            finally
            {
                if (!wasSuspended)
                {
                    GraphicalUiElement.IsAllLayoutSuspended = false;
                }
            }

            if (!wasSuspended)
            {
                toReturn.UpdateFontRecursive();
                toReturn.UpdateLayout();
            }
        }

        /// <summary>
        /// Invoked for every GraphicalUiElement after its owning element's SetInitialState has run,
        /// giving a hosting layer (like Gum.Forms) a chance to re-sync any Forms control state that
        /// was overridden by parent-level variable application. Set by <c>FormsUtilities</c>.
        /// </summary>
        public static Action<GraphicalUiElement>? InitialStateAppliedNotifier;

        private static void NotifyFormsControlsOfInitialStateApplied(GraphicalUiElement element)
        {
            var notifier = InitialStateAppliedNotifier;
            if (notifier == null) return;

            if (element.Children != null)
            {
                foreach (var child in element.Children)
                {
                    if (child is GraphicalUiElement gueChild)
                    {
                        notifier(gueChild);
                        NotifyFormsControlsOfInitialStateApplied(gueChild);
                    }
                }
            }
        }

        public static void CreateChildrenRecursively(GraphicalUiElement graphicalUiElement, ElementSave elementSave, ISystemManagers systemManagers)
        {
            bool isScreen = elementSave is ScreenSave;

            foreach (var instance in elementSave.Instances)
            {
                var childGue = instance.ToGraphicalUiElement(systemManagers);

                if (childGue != null)
                {
                    if (!isScreen)
                    {
                        childGue.Parent = graphicalUiElement;
                    }
                    childGue.ElementGueContainingThis = graphicalUiElement;
                }
            }
        }

        public static void Reset()
        {
            ElementTypeRegistry.Base.Clear();
            ReadLayer?.Clear();
        }
    }
}
