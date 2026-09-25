import Cartesia from "@cartesia/cartesia-js";
import { readFile, writeFile } from "node:fs/promises";

// A one-shot caller can pipe the key through stdin, keeping it out of the
// command line, process environment, source tree, and shell history.
const token = process.argv.includes("--stdin-key")
  ? (await readFile(0, "utf8")).trim()
  : process.env.CARTESIA_API_KEY;
const voice = process.env.CARTESIA_VOICE_ID;
if (
  !/^sk_car_[A-Za-z0-9_-]{8,}$/.test(token ?? "") ||
  token.length > 128 ||
  !/^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i.test(voice ?? "")
) {
  throw new Error("Provide a Cartesia key through --stdin-key or CARTESIA_API_KEY, and set CARTESIA_VOICE_ID to a reviewed voice.");
}

// Keep this text aligned with the three acts in src/story.tsx. Listen and review
// the resulting file before rendering or publishing either themed video.
const transcript = "Your picker keeps the full history. Find a copy, then flag it sensitive. Agent tools omit flagged items from recent, search, and direct reads.";
const client = new Cartesia({ token });
let response;
try {
  response = await client.tts.generate({
    model_id: "sonic-3.6-2026-08-27",
    voice,
    transcript,
    output_format: { container: "wav", encoding: "pcm_f32le", sample_rate: 44100 },
  });
} catch {
  throw new Error("Cartesia narration request failed. No audio was written.");
}
const audio = await response.blob();
await writeFile(new URL("./public/narration.wav", import.meta.url), Buffer.from(await audio.arrayBuffer()));
console.log("Saved public/narration.wav. Review pronunciation, pacing, and duration before rendering.");
