/** @type {import('tailwindcss').Config} */
module.exports = {
    content: ["./**/*.{razor,html}"],
    safelist: [
        'bg-primary', 'bg-secondary', 'bg-accent',
    ],
    theme: {
        extend: {},
    },
    // add daisyUI plugin
    plugins: [require('daisyui'),],
    // daisyUI config
    daisyui: {
        themes: [
            {
                light: {
                    ...require("daisyui/src/theming/themes")["light"],
                    "primary": "#AD83EC",
                    "secondary": "#EC83DC",
                    "accent": "#C2EC83",
                    ".card": {
                        "color": "#0b0613",
                    },
                    ".alert-success": {
                        "--alert-bg": "#83EC93",
                    },
                    ".alert-warning": {
                        "--alert-bg": "#C2EC83",
                    },
                    ".alert-error": {
                        "--alert-bg": "#EC83DC",
                    },
                },
                dark: {
                    ...require("daisyui/src/theming/themes")["dark"],
                    "primary": "#AD83EC",
                    "secondary": "#EC83DC",
                    "accent": "#C2EC83",
                    // Override base background colors for dark mode
                    "base-100": "#121212", // Darker background
                    ".card": {
                        "color": "#e6e6e6", // Light text for cards
                    },
                    "h2": {
                        "color": "#e6e6e6", // Set h2 headers to light color in dark mode
                    },
                    ".alert-success": {
                        "--alert-bg": "#83EC93",
                    },
                    ".alert-warning": {
                        "--alert-bg": "#C2EC83",
                    },
                    ".alert-error": {
                        "--alert-bg": "#EC83DC",
                    }
                }
            },
        ],
        darkTheme: "dark"
    },
}