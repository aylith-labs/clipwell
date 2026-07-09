// Lucide icons (lucide-solid 1.23.0) via per-icon deep imports so Vite bundles
// only what's used. This module is the single icon surface and the parity
// contract with the Avalonia picker (ui/Icons.cs transcribes the same lucide
// 1.23.0 geometry) — change an icon here and there together.
import type { LucideIcon } from "lucide-solid";
import Activity from "lucide-solid/icons/activity";
import ClipboardPaste from "lucide-solid/icons/clipboard-paste";
import Code from "lucide-solid/icons/code";
import Copy from "lucide-solid/icons/copy";
import EllipsisVertical from "lucide-solid/icons/ellipsis-vertical";
import ExternalLink from "lucide-solid/icons/external-link";
import FileText from "lucide-solid/icons/file-text";
import Folder from "lucide-solid/icons/folder";
import GitPullRequest from "lucide-solid/icons/git-pull-request";
import Globe from "lucide-solid/icons/globe";
import ImageIcon from "lucide-solid/icons/image";
import LinkIcon from "lucide-solid/icons/link";
import Lock from "lucide-solid/icons/lock";
import Mail from "lucide-solid/icons/mail";
import Palette from "lucide-solid/icons/palette";
import PanelRight from "lucide-solid/icons/panel-right";
import Pencil from "lucide-solid/icons/pencil";
import Pin from "lucide-solid/icons/pin";
import RefreshCw from "lucide-solid/icons/refresh-cw";
import Rows3 from "lucide-solid/icons/rows-3";
import Search from "lucide-solid/icons/search";
import Settings from "lucide-solid/icons/settings";
import SquarePen from "lucide-solid/icons/square-pen";
import Ticket from "lucide-solid/icons/ticket";
import Trash2 from "lucide-solid/icons/trash-2";
import TriangleAlert from "lucide-solid/icons/triangle-alert";

export type IconComponent = LucideIcon;

const KIND_ICONS: Record<string, IconComponent> = {
  "github-pr": GitPullRequest,
  "jira-issue": Ticket,
  url: LinkIcon,
  email: Mail,
  color: Palette,
  path: Folder,
  code: Code,
  image: ImageIcon,
  text: FileText,
};

/** The row icon for a detector kind (shared map with the Avalonia picker). */
export function kindIcon(kind: string | null): IconComponent {
  return KIND_ICONS[kind ?? "text"] ?? FileText;
}

/** Palette-action icons, keyed by action id (lib/actions.ts). */
export const ACTION_ICONS: Record<string, IconComponent> = {
  "open-url": ExternalLink,
  "open-path": Folder,
  copy: Copy,
  "copy-host": Globe,
  "paste-plain": ClipboardPaste,
  edit: SquarePen,
};

export {
  Activity,
  EllipsisVertical,
  Lock,
  PanelRight,
  Pencil,
  Pin,
  RefreshCw,
  Rows3,
  Search,
  Settings,
  SquarePen,
  Trash2,
  TriangleAlert,
};
