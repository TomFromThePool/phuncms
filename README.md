phuncms
======

Quickly add CMS capability to your new or existing Asp.NET MVC 4 project.

goal
======
Provide a simple way to add content management capability to any Asp.NET MVC 4 project.

Base on the modular CMS movement
   - utilize createjs client-side CMS ui for support
   - server CRUD interface/repository/connector for content storage
   - basic server + client interface for file management

![Architecture](http://i.imgur.com/chzYYGN.png)

howto 
=======
 - https://github.com/noogen/phuncms/wiki/How-to
 
quick start
========
 - You can use custom helpers to render partial content and scripts.
 - Content get server-side rendered when using HtmlHelper.

```c#
@Html.PhunRenderPartialContent("LeftHeader") 
```
or

```c#
@Html.PhunRenderPartialForInlineEdit("h2", "LeftHeader", new { @class= "one" })

@section scripts
{
    @Html.PhunRenderBundles()
}
```

from any html page
=========
```html
<div data-cmscontent="LeftHeader"></div><div data-cmscontent="RightHeader"></div>
```
- The contents are ajax loaded and will become inline editable for content admin.

```html
<div data-cmscontent="LeftHeader">%LeftHeader%</div><div data-cmscontent="RightHeader"></div>
```
- Example will render LeftHeader on server-side, while RightHeader get ajax load.
- Because both contains data-cmscontent, both are inline editable.

demo
========
- http://phuncms.azurewebsites.net/
- For now, go ahead and try it.  Grab the source and run Phun.Demo.Web or try out the nuget PhunCMS package.  Project requires Visual Studio 2012.
- Drop a comment, suggestion or request on github issue page.
