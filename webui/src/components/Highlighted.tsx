import { createMemo, For } from "solid-js";

/**
 * Renders text with every case-insensitive occurrence of the query marked.
 * Plain indexOf split (search is a substring match, mirroring the filter), so
 * no regex escaping and no innerHTML. Never used for masked sensitive rows.
 */
export function Highlighted(props: { text: string; query: string }) {
  const parts = createMemo(() => {
    const query = props.query.trim().toLowerCase();
    if (!query) return [{ text: props.text, hit: false }];
    const lower = props.text.toLowerCase();
    const out: { text: string; hit: boolean }[] = [];
    let position = 0;
    while (position <= props.text.length) {
      const hit = lower.indexOf(query, position);
      if (hit < 0) {
        out.push({ text: props.text.slice(position), hit: false });
        break;
      }
      if (hit > position) out.push({ text: props.text.slice(position, hit), hit: false });
      out.push({ text: props.text.slice(hit, hit + query.length), hit: true });
      position = hit + query.length;
    }
    return out;
  });
  return (
    <For each={parts()}>
      {(part) =>
        part.hit ? (
          <mark class="rounded-[3px] bg-amber-400/50 px-px text-inherit dark:bg-amber-400/35">
            {part.text}
          </mark>
        ) : (
          part.text
        )
      }
    </For>
  );
}
