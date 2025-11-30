# Design System with Radix UI and Tailwind CSS

This project implements a centralized design system using Radix UI colors and Tailwind CSS. The design system allows you to define and customize design tokens such as colors, spacing, and border radiuses from a single location.

## Features

- 🎨 **Centralized Design Tokens**: All design tokens are defined in a single location, making it easy to maintain and update.
- 🌓 **Dark Mode Support**: Built-in dark mode with system preference detection.
- 🎭 **Radix UI Integration**: Uses Radix UI colors and components for accessible UI elements.
- 🧩 **Customizable Components**: Pre-built components that follow the design system.
- 🔄 **Theme Switching**: Easy theme switching with persistent preferences.

## Design Tokens

The design system includes the following tokens:

### Colors

- **Primary** (blue): Used for primary actions and focus states
- **Success** (green): Used for success states and actions
- **Error** (red): Used for error states and destructive actions
- **Accent** (purple): Used for highlighting and accent elements
- **Neutral** (mauve): Used for text, backgrounds, and borders

Each color has 12 shades, from 1 (lightest) to 12 (darkest), following the Radix UI color scale.

### Spacing

Consistent spacing system based on rem units, from 0 to 96 (24rem).

### Border Radius

Consistent border radius tokens, from none to full (9999px).

### Typography

Font sizes, weights, and line heights are also defined as tokens.

## Usage

### Theme Provider

Wrap your application with the `ThemeProvider` to enable theme switching:

```tsx
import { ThemeProvider } from './providers/ThemeProvider';

function App() {
  return (
    <ThemeProvider>
      <YourApp />
    </ThemeProvider>
  );
}
```

### Theme Toggle

Use the `ThemeToggle` component to allow users to switch between light and dark mode:

```tsx
import { ThemeToggle } from './components/ThemeToggle';

function Header() {
  return (
    <header>
      <ThemeToggle />
    </header>
  );
}
```

### Using Design Tokens in Components

The design tokens are available as Tailwind CSS classes:

```tsx
<div className="bg-primary text-primary-foreground p-4 rounded-lg">
  This uses the primary color for background and appropriate foreground text color.
</div>
```

## Customization

To customize the design tokens, edit the following files:

- `src/styles/tokens.ts`: Contains all the design tokens definitions
- `src/styles/globals.css`: Applies the design tokens as CSS variables
- `tailwind.config.js`: Configures Tailwind CSS to use the design tokens

## Components

The design system includes pre-built components like:

- Button
- ThemeToggle
- (Add more as you develop them)

## Getting Started

```bash
# Install dependencies
npm install

# Run the development server
npm run dev
```
