# Product
<!-- impeccable:product-schema 1 -->
## Platform
Native Windows desktop.
## Stack
C# / WPF editor plus independently built C++ helper. User explicitly requires no Windhawk runtime dependency. Referenced Windhawk/other source retains applicable MIT/GPL provenance and obligations.
## Users
Zuzong, customizing the Windows shell with low resource use as a priority.
## Product Purpose
Adjust the real taskbar and related shell panels with liquid-glass refraction and blur while retaining native icons, controls, values and behavior.
## Capabilities and Constraints
Current user requirements: two separate taskbar pills for the application area and system tray; GPU glass for Quick Settings and taskbar thumbnails; less-rounded panels; horizontal liquid-glass capsule thumbs for volume and brightness that refract background and track while dragging. Water-drop-shaped thumbs are explicitly rejected.

GPU mode uses one shared Windows Graphics Capture session, two full-primary-monitor frame-pool buffers and a cached full-monitor texture. Visible surfaces own independent swapchains, region bitmaps and geometry resources. The slider renderer builds an offscreen background-and-track scene from native slider geometry, then applies capsule lens displacement. Shared capture and cropped surface rendering do not establish a tiny memory footprint. Final RAM/VRAM/GPU benchmarks are pending.

Lightweight mode uses native Acrylic with Windows-controlled blur. Primary screen only. Portable Data folder is shared between editor and helper; reopening an EXE attaches to the active helper's Data path. The editor can close fully while the helper continues; no separate tray icon. No system-file patch or automatic Explorer restart.
## Brand Commitments
Vietnamese UI; user-pinned macOS liquid glass and supplied references. Two taskbar pills, restrained panel rounding, horizontal symmetric capsule slider thumbs, never water drops.
## Evidence on Hand
Environment: Windows build 26300, one 2560x1600 monitor. Earlier taskbar GPU frames establish only the earlier displacement/blur/tint/rim implementation. The standalone dock prototype was rejected.

Private GPU lab passed 200 property update/restore cycles and 10 popup open/close/move cycles with two independent surfaces and one capture session. Spring self-check measured peak 1.072263 and settled 1.000000. Native v13 passed mode/Stop/restore checks. Final v14 changes paired toggle rounding and editor wording. Native Quick/thumbnail drag experience and long-term stability still need user verification. Prototype v10 crashes were traced to null active-surface and cross-thread non-agile property record ownership; current records remain on their owning UI thread. Do not claim all flicker is solved; user suspects a pink computer-control overlay, and subsequent verification avoids mouse/screenshot automation.

Capture exclusion can hide taskbar, Quick Settings, thumbnails and overlays from screenshots, recordings and sharing. GPU dumps/offscreen lab images are not full-shell screenshots and must be labeled at their actual scope.
## Product Principles
Keep Windows blur, capture visibility and resource cost clear. Measure host shell process deltas and GPU activity, including full-monitor buffers and caches, rather than editor RAM alone. Keep XAML property objects and restoration on their owning UI threads. Restore original properties and capture affinity when disabled. Do not claim shell success from code inspection, compilation or private lab rendering.

Elastic capsule: native input values are unchanged; GPU spring uses k=625, damping=30, integration steps <=1/120 s, bounded visual lag, full reset on reopen. Toggle rectangles use 8 DIP outside corners.


Current V13/engine18 update: separate Quick and Thumbnail material controls (refraction/blur/radius/tint/bezel/rim); slider expansion, bounce and dedicated refraction. UI opens on Quick Settings. Geometry polls at 33ms while active and wakes renderer on changes; removed fixed 220/90ms gates. Cross-shell exchange uses explicitly file-backed mapped views with OS writer locks and seqlock readers, including interrupted-writer recovery. Quick/thumbnail capture exclusion is mandatory to avoid reflecting their own controls, independent of taskbar capture preference. Native thumb visual hides only after GPU slider readiness; native input is retained. Native Quick verified two valid slider geometries and ready=true, own GPU/UI exports show separate backgrounds and a single displayed slider layer. Final visual satisfaction and full native latency remain unconfirmed; do not reuse the v15 lab 50ms mean/78ms max as a final native latency claim. Source review passed live redraw after settings-only changes.


V14 / engine19: Quick lighting checkbox controls cached panel rim/specular reflection and lens rim, with QuickRim intensity; taskbar lighting is unchanged. Volume Mixer capacity rises from 2 to 32 visible sliders; ready contains exact successfully drawn IDs, so only covered native rows hide. Cached native parts, per-row exception cleanup, ancestor visibility/viewport filtering and one host query reduce scanning work; lens maps capped16 and brush resources reused. Springs reset on slider identity changes. Empty publications from the owning UI thread clear immediately. Private 4-slider lab passes all four individually ready IDs, interrupted publisher test and lens cache eviction/dimension tests. Native observation has reached three validated sliders. Full native visual satisfaction, all mixer configurations, and clean-process resource profiling remain unverified.
