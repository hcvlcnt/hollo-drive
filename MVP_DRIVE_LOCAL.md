# MVP para um Drive local

## Objetivo do produto

Transformar o Hollo em um "Drive" local para computador, rede interna ou servidor local, onde usuarios consigam armazenar, organizar, baixar e administrar arquivos com seguranca basica.

O MVP nao precisa competir com Google Drive/OneDrive. Ele precisa provar o fluxo essencial:

- [ ] Um usuario entra no sistema.
- [ ] Cria pastas.
- [ ] Envia arquivos.
- [ ] Navega pelos arquivos.
- [ ] Baixa arquivos.
- [ ] Remove/recupera itens.
- [ ] Um administrador consegue operar usuarios, quotas e armazenamento local com confianca.

## O que o projeto ja tem

- [x] Frontend React com tela principal de gerenciador de arquivos.
- [x] Backend ASP.NET com autenticacao via cookie/JWT.
- [x] Cadastro, login, logout e usuario administrador padrao.
- [x] Modelo de usuarios e papeis.
- [x] Modelo de arquivos e pastas.
- [x] Listagem de diretorio com breadcrumbs.
- [x] Criacao de pastas.
- [x] Upload de arquivos usando URL SAS.
- [x] Download de arquivos usando URL SAS.
- [x] Calculo basico de uso de armazenamento por usuario e sistema.
- [x] Quota padrao por usuario.
- [x] Docker Compose com frontend, backend, PostgreSQL e Azurite.
- [x] Storage local de desenvolvimento via Azurite, persistido em volume Docker.
- [x] Endpoints de health check.

## Lacunas criticas para MVP

### 1. Excluir, lixeira e restaurar arquivos

Hoje existem campos `DeletedAt` e metodo de soft delete em arquivo, mas nao ha fluxo completo exposto e conectado.

Para MVP precisa:

- [x] Endpoint para mover arquivo para lixeira.
- [x] Endpoint para mover pasta para lixeira.
- [x] Endpoint para listar lixeira.
- [ ] Endpoint para restaurar arquivo/pasta.
- [ ] Endpoint para excluir definitivamente.
- [ ] Apagar o blob fisico quando a exclusao definitiva acontecer.
- [x] Atualizar a UI para a acao "Mover para lixeira" funcionar.
- [ ] Proteger contra exclusao de pasta com filhos sem regra clara.

Sem isso, o usuario consegue enviar arquivos mas nao consegue corrigir erros com seguranca.

### 2. Renomear conectado na interface

O backend ja tem endpoints para renomear arquivos e pastas, mas a UI mostra a acao sem executar.

Para MVP precisa:

- [ ] Chamar `PATCH /api/files/{id}/name`.
- [ ] Chamar `PATCH /api/files/folders/{id}/name`.
- [ ] Validar nome vazio.
- [ ] Mostrar erro amigavel.
- [ ] Atualizar cache/listagem apos renomear.

### 3. Backend de armazenamento local real

O projeto usa Azure Blob/Azurite. Isso funciona bem em Docker, mas o posicionamento do produto e "Drive local para computador/servidor local".

Para MVP precisa decidir e implementar um modo oficial:

- [ ] Manter Azurite como storage local recomendado.
- [ ] Adicionar um `LocalFileSystemStorageService` que grave em uma pasta configurada do servidor.
- [ ] Registrar a decisao final de storage para o MVP.

Recomendacao para MVP: manter Azurite se o alvo inicial for Docker/servidor local; criar `LocalFileSystemStorageService` se o alvo inicial for instalacao simples em um computador sem dependencia de Azurite.

Independente da escolha, documentar:

- [ ] Onde os arquivos ficam fisicamente.
- [ ] Como fazer backup.
- [ ] Como restaurar backup.
- [ ] Como trocar a pasta/volume de dados.
- [ ] Limites de tamanho.

### 4. Consistencia entre upload de blob e metadata no banco

O fluxo atual envia o blob primeiro e depois cria metadata. Se o segundo passo falhar, pode sobrar arquivo no storage sem registro no banco.

Para MVP precisa:

- [ ] Criar rotina de limpeza de blobs orfaos.
- [ ] Ou criar fluxo de upload pendente, confirmar apos upload e expirar pendencias.
- [ ] Registrar erros de upload/metadata para auditoria.

Sem isso, o armazenamento pode crescer sem o usuario ver os arquivos.

### 5. Busca real na UI

O backend tem endpoint de busca, mas a tela principal nao usa busca funcional.

Para MVP precisa:

- [ ] Campo de busca conectado ao backend.
- [ ] Busca por nome de arquivo/pasta.
- [ ] Estado vazio.
- [ ] Loading.
- [ ] Limpar busca e voltar ao diretorio atual.
- [ ] Definir se a busca sera dentro da pasta atual, global ou ambos.

### 6. Administracao minima de usuarios na interface

O backend tem endpoints administrativos de usuarios, mas nao ha painel no frontend.

Para MVP precisa:

- [ ] Tela de usuarios para admin.
- [ ] Listar usuarios.
- [ ] Criar/editar usuario ou pelo menos editar nome, email, role e status.
- [ ] Desativar usuario.
- [ ] Visualizar uso de armazenamento por usuario.
- [ ] Impedir que admin apague a si mesmo, como o backend ja protege.

Sem isso, o sistema fica dificil de operar em um servidor local compartilhado.

### 7. Instalacao e operacao documentadas

Para um Drive local, a instalacao precisa ser previsivel.

Para MVP precisa:

- [ ] README na raiz com requisitos.
- [ ] Comando de subida via Docker Compose.
- [ ] Credenciais iniciais.
- [ ] Portas usadas.
- [ ] Onde ficam PostgreSQL e arquivos.
- [ ] Como alterar senha do admin.
- [ ] Como configurar quota.
- [ ] Como fazer backup dos volumes.
- [ ] Como atualizar versao.
- [ ] Checklist de troubleshooting.

## Lacunas importantes, mas podem vir logo apos o MVP

### Compartilhamento

A UI mostra "Compartilhar", mas nao existe modelo completo de compartilhamento.

Opcoes:

- [ ] Decidir se compartilhamento fica fora do MVP.
- [ ] Remover/desabilitar a acao visual se ficar fora do MVP.
- [ ] Ou implementar compartilhamento simples por link com token e expiracao.

Para MVP eu recomendo remover/desabilitar visualmente se nao for implementar. Mostrar um recurso que nao funciona reduz confianca.

### Favoritos/estrelas e recentes

A UI mostra "Com estrela", "Recentes" e estrelas nos cards, mas nao ha persistencia ou filtros reais.

Para MVP:

- [ ] Decidir se favoritos/estrelas/recentes ficam fora do MVP.
- [ ] Remover/desabilitar se ficarem fora do MVP.
- [ ] Ou implementar campos, filtros e endpoints.

Recomendacao: deixar fora do MVP e focar no fluxo de arquivos.

### Mover arquivos e pastas

Um Drive normalmente precisa mover itens entre pastas.

Para MVP minimo pode ficar fora, mas seria muito util incluir:

- [ ] Endpoint para mover arquivo.
- [ ] Endpoint para mover pasta.
- [ ] Validacao para impedir mover uma pasta para dentro dela mesma.
- [ ] UI com seletor de destino.

### Upload de pastas

Hoje o upload e por arquivos selecionados. Para uso tipo Drive local, upload de pasta ajuda muito.

Pode ficar pos-MVP, mas quando entrar precisa:

- [ ] Preservar estrutura relativa.
- [ ] Criar pastas automaticamente.
- [ ] Lidar com nomes duplicados.

### Preview de arquivos

Nao e obrigatorio para MVP. Download ja resolve o fluxo principal.

Entraria depois para:

- [ ] Imagens.
- [ ] PDF.
- [ ] Texto.
- [ ] Video/audio no navegador.

### Auditoria e logs

Para servidor local compartilhado, e importante saber quem fez o que.

Pos-MVP:

- [ ] Registrar upload, download, rename, delete, restore.
- [ ] Exibir historico para admin.
- [ ] Guardar IP/user agent se fizer sentido.

## Requisitos tecnicos de MVP

### Backend

- [ ] Endpoints completos para lixeira.
- [ ] Endpoints conectados para renomear.
- [ ] Remocao definitiva apagando metadata e blob.
- [ ] Politica clara de duplicidade de nomes para arquivos e pastas.
- [ ] Validacao de tamanho por arquivo e quota por usuario.
- [ ] Tratamento de erros padronizado.
- [ ] Storage local oficial documentado.
- [ ] Rotina ou estrategia para blobs sem metadata.
- [ ] Testes basicos para autenticacao, upload metadata, browse, rename e delete.

### Frontend

- [ ] Acoes reais no menu de arquivo/pasta.
- [ ] Dialogs melhores que `window.prompt` para criar pasta e renomear.
- [ ] Busca conectada.
- [ ] Tela de lixeira.
- [ ] Painel admin basico.
- [ ] Mensagens de erro/sucesso consistentes.
- [ ] Loading states por acao.
- [ ] Remover ou desabilitar acoes ainda nao implementadas.

### Infra e operacao

- [ ] README raiz.
- [ ] `.env.example` ou documentacao de variaveis.
- [ ] Volumes persistentes claros.
- [ ] Backup/restore documentado.
- [ ] Credencial admin inicial configuravel.
- [ ] Health check do storage alem do banco.
- [ ] Build de producao validado via Docker Compose.

## Escopo sugerido do MVP

### Deve entrar

- [x] Login/logout.
- [x] Admin inicial.
- [x] Criar pastas.
- [x] Upload de multiplos arquivos.
- [x] Navegar por pastas.
- [x] Download de arquivos.
- [ ] Renomear arquivos e pastas.
- [ ] Mover para lixeira.
- [ ] Restaurar da lixeira.
- [ ] Excluir definitivamente.
- [x] Quota por usuario.
- [ ] Painel admin minimo para listar/desativar usuarios e ver uso.
- [ ] Documentacao de instalacao local.
- [ ] Backup e restore documentados.

### Deve sair do MVP

- [ ] Compartilhamento publico.
- [ ] Favoritos/estrelas.
- [ ] Recentes.
- [ ] Preview de documentos.
- [ ] Sincronizacao desktop.
- [ ] Versionamento de arquivos.
- [ ] Comentarios.
- [ ] Permissoes granulares por pasta.
- [ ] Upload resumivel/chunked.

## Criterios de aceite do MVP

- [ ] Um usuario novo consegue entrar, criar uma pasta, enviar um arquivo, baixar o arquivo e renomea-lo.
- [ ] Um usuario consegue remover um arquivo, ve-lo na lixeira, restaurar e excluir definitivamente.
- [ ] Um arquivo excluido definitivamente tambem deixa de ocupar storage fisico.
- [ ] Um usuario nao consegue ver arquivos de outro usuario.
- [ ] Um usuario nao consegue ultrapassar a quota configurada.
- [ ] Um admin consegue ver usuarios e uso de armazenamento.
- [ ] O sistema sobe com um unico `docker compose up`.
- [ ] A documentacao explica onde os dados ficam e como fazer backup.
- [ ] As acoes visiveis na UI funcionam ou estao explicitamente desabilitadas.

## Ordem recomendada de implementacao

- [ ] Conectar renomear no frontend.
- [ ] Implementar lixeira completa no backend.
- [ ] Conectar lixeira no frontend.
- [ ] Implementar exclusao definitiva com remocao do blob.
- [ ] Padronizar erros e loading states.
- [ ] Conectar busca.
- [ ] Criar painel admin minimo.
- [ ] Documentar instalacao, storage, backup e restore.
- [ ] Adicionar testes dos fluxos principais.
- [ ] Remover/desabilitar recursos visuais que ficarem fora do MVP.

## Observacao sobre o nome "Drive local"

Se a promessa for "roda em servidor local via Docker", o desenho atual com PostgreSQL + Azurite faz sentido para MVP.

Se a promessa for "instala no computador e escolhe uma pasta do disco", falta um modo de armazenamento por filesystem local. Essa decisao deve ser tomada antes de fechar o MVP, porque muda instalacao, backup, permissao de arquivos e suporte.
