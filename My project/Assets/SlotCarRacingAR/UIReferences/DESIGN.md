---
name: Turbo Retro
colors:
  surface: '#fcf9f8'
  surface-dim: '#dcd9d9'
  surface-bright: '#fcf9f8'
  surface-container-lowest: '#ffffff'
  surface-container-low: '#f6f3f2'
  surface-container: '#f0eded'
  surface-container-high: '#eae7e7'
  surface-container-highest: '#e5e2e1'
  on-surface: '#1c1b1b'
  on-surface-variant: '#49473e'
  inverse-surface: '#313030'
  inverse-on-surface: '#f3f0ef'
  outline: '#7a776d'
  outline-variant: '#cbc6bb'
  surface-tint: '#635e4d'
  primary: '#635e4d'
  on-primary: '#ffffff'
  primary-container: '#eae3cd'
  on-primary-container: '#696553'
  inverse-primary: '#cdc6b1'
  secondary: '#34647c'
  on-secondary: '#ffffff'
  secondary-container: '#b1e0fd'
  on-secondary-container: '#34647d'
  tertiary: '#b4271b'
  on-tertiary: '#ffffff'
  tertiary-container: '#ffdbd5'
  on-tertiary-container: '#bd2e21'
  error: '#ba1a1a'
  on-error: '#ffffff'
  error-container: '#ffdad6'
  on-error-container: '#93000a'
  primary-fixed: '#e9e2cc'
  primary-fixed-dim: '#cdc6b1'
  on-primary-fixed: '#1e1c0e'
  on-primary-fixed-variant: '#4b4737'
  secondary-fixed: '#c3e8ff'
  secondary-fixed-dim: '#9ecde9'
  on-secondary-fixed: '#001e2c'
  on-secondary-fixed-variant: '#174c63'
  tertiary-fixed: '#ffdad5'
  tertiary-fixed-dim: '#ffb4a8'
  on-tertiary-fixed: '#410000'
  on-tertiary-fixed-variant: '#910905'
  background: '#fcf9f8'
  on-background: '#1c1b1b'
  surface-variant: '#e5e2e1'
typography:
  display-xl:
    fontFamily: Anybody
    fontSize: 72px
    fontWeight: '900'
    lineHeight: 80px
    letterSpacing: -0.02em
  headline-lg:
    fontFamily: Anybody
    fontSize: 32px
    fontWeight: '800'
    lineHeight: 40px
    letterSpacing: 0.05em
  headline-md:
    fontFamily: Chivo
    fontSize: 24px
    fontWeight: '800'
    lineHeight: 32px
  body-lg:
    fontFamily: Chivo
    fontSize: 18px
    fontWeight: '400'
    lineHeight: 24px
  label-bold:
    fontFamily: Chivo
    fontSize: 14px
    fontWeight: '900'
    lineHeight: 20px
    letterSpacing: 0.1em
rounded:
  sm: 0.25rem
  DEFAULT: 0.5rem
  md: 0.75rem
  lg: 1rem
  xl: 1.5rem
  full: 9999px
spacing:
  margin-safe: 2rem
  gutter: 1rem
  border-thick: 3px
  border-thin: 1.5px
---

## Brand & Style

The visual identity is a high-octane blend of 90s arcade racing and comic book aesthetics. It is designed to evoke nostalgia, excitement, and immediate kinetic energy. The target audience includes casual gamers and retro enthusiasts who appreciate a "hands-on," tactile feel in a digital interface.

The design system employs a **Brutalism-meets-Retro** style. It utilizes heavy black outlines (keylines), high-contrast color blocks, and exaggerated proportions to ensure every UI element feels like a physical piece of a vintage racing machine. The use of checkered patterns and speed lines reinforces the racing theme, creating a playful yet structured environment.

## Colors

The palette is anchored in a vintage "Cream" base to reduce harshness and provide a sophisticated retro backdrop. 

- **Cream (#EAE3CD):** Used for the main background to evoke aged paper or vintage plastic.
- **Deep Teal (#23556D):** The primary container color, providing a solid, trustworthy base for panels and buttons.
- **Vibrant Red (#D64030):** Used for primary actions, critical alerts, and the "Host" team identity.
- **Racing Yellow (#F9B731):** Reserved for highlights, session codes, and secondary call-to-actions.
- **Pitch Black (#1A1A1A):** Used exclusively for heavy borders, drop shadows, and high-contrast typography.

## Typography

The typography is aggressive and italicized to simulate movement. All display and headline text must feature a **2px to 4px black stroke** or a hard-edge black drop shadow to ensure legibility against complex backgrounds.

- **Display Text:** Used for large countdowns and titles. It should always be uppercase and italicized.
- **Body Text:** Uses a clean, modern sans-serif for readability in instructions and sub-headers.
- **Labels:** Heavily tracked and bolded to provide a "technical" or "dashboard" feel.

## Layout & Spacing

This design system is optimized for a **16:9 horizontal orientation**. The layout utilizes a fixed grid approach where elements are grouped into centralized "pods" or cards to maintain focus during high-speed gameplay.

- **Safe Zones:** A 32px (2rem) margin is enforced around the screen perimeter to avoid hardware cutouts and thumb interference.
- **Alignment:** Centralized containers are preferred for lobby screens, while corners are utilized for HUD (Heads-Up Display) elements like lap counters and positioning.
- **Checkered Motifs:** Diagonal checkered bands should be placed in opposite corners to lead the eye toward the center of the screen.

## Elevation & Depth

Depth is not achieved through light and physics, but through **Graphic Stacking**.

- **Hard Shadows:** Instead of blurs, use 100% opaque black offsets (usually 4px down and 4px right) for buttons and cards.
- **Layered Containers:** Containers often feature an inner "inset" look, achieved by using a slightly darker version of the surface color at the bottom edge.
- **Keylining:** Every interactive element must have a thick, black 3px border to separate it from the background.

## Shapes

The shape language balances "industrial" and "friendly." While panels are generally rectangular, they feature **generous 16px to 24px corner radii** to prevent the UI from feeling too sharp or aggressive.

- **Asymmetric Tabs:** Use 45-degree corner snips on panels to give them a "futuristic cockpit" or "license plate" aesthetic.
- **Circular Indicators:** Status lights and player markers should be perfect circles with heavy strokes.

## Components

### Buttons
Primary buttons should be Red or Yellow with a thick black outline and a 4px hard-drop shadow. Upon press, the button should shift 2px down and right, and the shadow should disappear to simulate a physical "click."

### Cards & Panels
Main panels use the Deep Teal background. Sub-panels (like text inputs) use the Cream background with an internal shadow to indicate a "recessed" area.

### Indicators (Status Lights)
Use vibrant Green for "Ready" and Red for "Busy." These should include a subtle radial gradient to mimic a glowing LED bulb.

### Checkered Flags
Used as decorative accents. The squares should be roughly 16px x 16px. They can be used as dividers or to "frame" the main title of a screen.

### Icons
Icons must be thick-stroked vector graphics. Avoid fine lines; icons should be legible at small sizes (24px) even with a heavy border.