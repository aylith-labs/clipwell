import Cartesia from "@cartesia/cartesia-js";
import { writeFile } from "node:fs/promises";

const token = process.env.CARTESIA_API_KEY;
const voice = process.env.CARTESIA_VOICE_ID;
if (!token || !voice) {
  throw new Error("Set CARTESIA_API_KEY and CARTESIA_VOICE_ID to a reviewed Cartesia voice before generating audio.");
}

// Keep this text aligned with the three acts in src/story.tsx. Listen and review
// the resulting file before rendering or publishing either themed video.
const transcript = "Your picker keeps the full history. Find a copy, then flag it sensitive. Agent tools omit flagged items from recent, search, and direct reads.";
const client = new Cartesia({ token });
const response = await client.tts.generate({
  model_id: "sonic-latest",
  voice,
  transcript,
  output_format: { container: "wav", encoding: "pcm_f32le", sample_rate: 44100 },
});
const audio = await response.blob();
await writeFile(new URL("./public/narration.wav", import.meta.url), Buffer.from(await audio.arrayBuffer()));
console.log("Saved public/narration.wav. Review pronunciation, pacing, and duration before rendering.");
