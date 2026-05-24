export default {
  content: ["./index.html", "./src/**/*.{js,jsx,ts,tsx}"],
  corePlugins: {
    preflight: false, // Tránh xung đột với Ant Design
  },
  theme: {
    extend: {},
  },
  plugins: [],
};
