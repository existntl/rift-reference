# Rift Ready branding

## Current white-and-teal mark (0.9.2)

The second user-supplied board supersedes the all-mint mark. The lower-left leg and
inner diagonal are off white #E9F1EE; the upper bowl and right leg are teal #00E5C2.
Keep the accepted charcoal UI palette. The new transparent asset replaces the same
embedded app resource and flows through app/installer icons and mobile HTML.
Website uses cache-safe rift-ready-v2.png and rift-ready-v2.ico URLs.

Reference: attachment 90aa3888-89e4-48e8-9b8c-0a813fa8639b/1-Photo-1.jpg.
Generated source: exec-b8135140-f100-4f48-8a04-99437ce38f29.png, copied to
assets/branding/rift-ready.png. Chosen image-generation prompt:
“Create a production-ready isolated logo asset faithfully matching the large RR
monogram in the UPPER LEFT of the attached NEW Rift Ready brand board. Exact angular
interlocking R mark with diagonal negative-space slash: lower-left leg and descending
inner diagonal are OFF WHITE (#E9F1EE), upper horizontal/angular bowl and long
bottom-right diagonal leg are vivid MINT TEAL (#00E5C2) with only a very subtle pale-mint
gradient at upper left. Preserve precise straight geometry, proportions and shape
from this new design. Clean antialiased edges, flat polished logo, not a photograph
or textured cutout. Square canvas, mark centered filling 84 percent. Actual transparent
alpha background, including all negative spaces. No text, no rounded tile, no borders,
no glow, no stray pixels, no shadows, no presentation board or mockups. This is an asset
to replace the previous all-teal logo in a Windows app and website.”

## Earlier board history

User correction: keep the Rift Ready name, logo and tagline, but restore the pre-rebrand
charcoal/teal UI palette on desktop and the previous website/mobile colors. The brand-board
palette below describes the supplied artwork, not the accepted interface theme.

The user supplied the Rift Ready brand board on 2026-09-06. The display name is now
Rift Ready, with the RR monogram and “Analyze · Plan · Climb” tagline. Use mint #5FE1C2,
deep green #0F3D36, charcoal #0B1211 and off-white #E9F1EE. Small UI text uses a lighter
muted green than the board's #647B76 to remain readable.

Logo: assets/branding/rift-ready.png. Prepared with the built-in image-generation tool
from the supplied board. Final chosen prompt: “Extract the exact angular interlocking
RR monogram from the large PRIMARY LOGO at the upper left of the supplied Rift Ready
branding board. Deliver one isolated mint-green monogram, square canvas with transparent
alpha background, centered and filling 85% of canvas. Preserve its exact silhouette,
angular cuts, negative spaces and diagonal slash. Preserve the mint-to-teal subtle
gradient. No text, no border, no app tile, no shadow, no glow, no branding board, no new
logo design.” A cleanup variant was inspected but not adopted. Inspect at intended UI
sizes; the supplied sheet remains the source of visual direction.

scripts/build-brand-icon.ps1 converts the asset to a multi-size Windows icon and a small
PNG. The app embeds the logo, app/installer icons use the ICO, and the mobile HTML embeds
the small PNG at build time. No additional mobile route or runtime image request exists.

Compatibility identities remain RiftReference: executable name, install marker product,
registry key, default directory, process detection, updater manifest product, GitHub
repository/download filenames and existing website address. Do not rename these blindly.
Saved plans, preferences, reviews and public-key continuity remain intact. Windows display
name and new shortcuts use Rift Ready. The installer removes a legacy named shortcut only
when its target matches this installation's executable.
