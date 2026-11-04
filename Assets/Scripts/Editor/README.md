# Optional Cubism editor integration

The base project does not require the Live2D SDK to compile. The editor assembly
has no reference to `Live2D.Cubism`; `CubismPickingBuildProcessor` resolves the
actual `Live2D.Cubism.Rendering.CubismRenderer` type from that assembly only when
processing a player build. When the SDK is absent, picking cleanup does nothing.
No scripting define, placeholder SDK type, or SDK installation is required.

With a separately authorized SDK installed under its normal assembly name, the
processor retains its previous behavior: it searches active and inactive objects
in player scene copies, removes only their picking `MeshFilter`, and keeps the
renderer components, enabled state, sorting and material property blocks. Scene
processing outside player builds remains unchanged. The SDK itself supplies any
optional render-pipeline adapters; the base pipeline and platform controller do
not reference SDK types.

Removing the SDK does not migrate serialized scene or prefab components. Existing
Live2D content can still report missing scripts or assets and cannot render without
the real SDK and authorized content. Do not resave those assets just to remove
missing-component warnings. Install and use third-party components only under
their applicable terms; this integration does not distribute or license the SDK.

Regression tests are the `CubismPickingBuildProcessor_*` cases in
`Assets/Tests/EditMode/Game/EditorTools/BuildHelperTests.cs`. The SDK-free case
requires the actual SDK to be absent. Traversal is checked using real Unity
`MeshRenderer` components, without introducing fake Live2D namespaces or assemblies.
