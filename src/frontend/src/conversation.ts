import type { ChatMessage } from './api/client'

// The Kérdezz conversation: it lives in App's memory, so it outlives the page (l3_l5_minispec L5).
export type Message = ChatMessage & { toolsUsed?: string[] }

// The id tells conversations apart: an answer that comes after "Új beszélgetés" or signing out is dropped, so it never
// reaches the next conversation or the next user.
export type Conversation = { id: number; messages: Message[]; pending: string | null; question: string; error: string | null }

let lastConversationId = 0
export function newConversation(): Conversation {
  lastConversationId += 1
  return { id: lastConversationId, messages: [], pending: null, question: '', error: null }
}
