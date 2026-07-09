/* @refresh reload */
import { render } from "solid-js/web";
import { App } from "./App";
import { isTauri } from "./lib/platform";
import "./styles.css";

// In the Tauri shell the window is transparent and the app draws its own
// rounded card (styles.css + the card wrapper class below).
if (isTauri()) document.documentElement.classList.add("tauri");

const root = document.getElementById("root");
if (!root) throw new Error("missing #root");

render(() => <App />, root);
